using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Observatory.Framework;
using Observatory.Framework.Files;
using Observatory.Framework.Interfaces;

namespace Observatory
{
    public class LogMonitor : ILogMonitor
    {
        readonly ILogger _logger;
        readonly IAppSettings _settings;

        public LogMonitor(ILogger<LogMonitor> logger, IAppSettings settings)
        {
            _logger = logger;
            _settings = settings;

            currentLine = new();
            journalTypes = JournalReader.PopulateEventClasses();
            InitializeWatchers(string.Empty);
            SetLogMonitorState(LogMonitorState.Idle);
        }

        #region Public properties
        public LogMonitorState CurrentState
        {
            get => currentState;
        }
        #endregion

        #region Public Methods

        public void Start()
        {
            if (firstStartMonitor)
            {
                // Only pre-read on first start monitor. Beyond that it's simply pause/resume.
                firstStartMonitor = false;
                if (_settings.StartReadAll)
                    ReadAll();
                else
                    ReadCurrent();
            }
            journalWatcher.EnableRaisingEvents = true;
            statusWatcher.EnableRaisingEvents = true;
            SetLogMonitorState(LogMonitorState.Realtime);
            JournalPoke();
        }

        public void Stop()
        {
            journalWatcher.EnableRaisingEvents = false;
            statusWatcher.EnableRaisingEvents = false;
            SetLogMonitorState(LogMonitorState.Idle);
        }

        public void ChangeWatchedDirectory(string path)
        {
            journalWatcher.Dispose();
            statusWatcher.Dispose();
            InitializeWatchers(path);
        }

        public bool IsMonitoring()
        {
            return currentState.HasFlag(LogMonitorState.Realtime);
        }

        public void ReadAll()
        {
            // Prevent pre-reading when starting monitoring after reading all.
            firstStartMonitor = false;
            SetLogMonitorState(currentState | LogMonitorState.Batch);

            DirectoryInfo logDirectory = GetJournalFolder(_settings.JournalFolder);
            var files = GetJournalFilesOrdered(logDirectory);
            foreach (var file in files)
            {
                foreach (var line in ReadAllLines(file.FullName))
                    DeserializeAndInvoke(file.Name, line);
            }

            SetLogMonitorState(currentState & ~LogMonitorState.Batch);
        }

        public async Task ReadAllAsync()
        {
            // Prevent pre-reading when starting monitoring after reading all.
            firstStartMonitor = false;
            SetLogMonitorState(currentState | LogMonitorState.Batch);

            DirectoryInfo logDirectory = GetJournalFolder(_settings.JournalFolder);
            var files = GetJournalFilesOrdered(logDirectory);
            foreach (var file in files)
            {
                await foreach(var line in  ReadAllLinesAsync(file.FullName))
                    DeserializeAndInvoke(file.Name, line);
            }

            SetLogMonitorState(currentState & ~LogMonitorState.Batch);
        }

        public Task ReadCurrentAsync()
        {
            return Task.Run(() => ReadCurrent());
        }

        public void ReadCurrent()
        {
            SetLogMonitorState(currentState | LogMonitorState.PreRead);

            DirectoryInfo logDirectory = GetJournalFolder(_settings.JournalFolder);
            var files = GetJournalFilesOrdered(logDirectory);
            
            // Read at most the last two files (in case we were launched after the game and the latest
            // journal is mostly empty) but keeping only the lines since the last FSDJump.
            List<String> lastSystemLines = new();
            List<String> lastFileLines = new();
            string lastLoadGame = String.Empty;
            bool sawFSDJump = false;
            foreach (var file in files.Skip(Math.Max(files.Count() - 2, 0)))
            {
                var lines = ReadAllLines(file.FullName);
                foreach (var line in lines)
                {
                    var eventType = JournalUtilities.GetEventType(line);
                    if (eventType.Equals("FSDJump") || (eventType.Equals("CarrierJump") && line.Contains("\"Docked\":true")))
                    {
                        // Reset, start collecting again.
                        lastSystemLines.Clear();
                        sawFSDJump = true;
                    }
                    else if (eventType.Equals("Fileheader"))
                    {
                        lastFileLines.Clear();
                    }
                    else if (eventType.Equals("LoadGame"))
                    {
                        lastLoadGame = line;
                    }
                    lastSystemLines.Add(line);
                    lastFileLines.Add(line);
                }
            }

            // If we didn't see a jump in the recent logs (Cmdr is stationary in a system for a while
            // ie. deep-space mining from a carrier), at very least, read from the beginning of the
            // current journal file which includes the important stuff like the last "LoadGame", etc. This
            // also helps out in cases where one forgets to hit "Start Monitor" until part-way into the
            // session (if auto-start is not enabled).
            List<string> linesToRead = lastFileLines;
            if (sawFSDJump)
            {
                // If we saw a LoadGame, insert it as well. This ensures odyssey biologicials are properly
                // counted/presented.
                if (!String.IsNullOrEmpty(lastLoadGame))
                {
                    lastSystemLines.Insert(0, lastLoadGame);
                }
                linesToRead = lastSystemLines;
            }

            foreach (var line in linesToRead)
                DeserializeAndInvoke("Pre-read", line);

            SetLogMonitorState(currentState & ~LogMonitorState.PreRead);
        }

        #endregion

        #region Public Events

        public event EventHandler<LogMonitorStateChangedEventArgs> LogMonitorStateChanged;

        public event EventHandler<JournalEventArgs> JournalEntry;

        public event EventHandler<JournalEventArgs> StatusUpdate;

        #endregion

        #region Private Fields

        private FileSystemWatcher journalWatcher;
        private FileSystemWatcher statusWatcher;
        private Dictionary<string, Type> journalTypes;
        private Dictionary<string, int> currentLine;
        private LogMonitorState currentState = LogMonitorState.Idle; // Change via #SetLogMonitorState
        private bool firstStartMonitor = true;
        private string[] EventsWithAncillaryFile = new string[] 
        { 
            "Cargo", 
            "NavRoute", 
            "Market", 
            "Outfitting", 
            "Shipyard", 
            "Backpack", 
            "FCMaterials",
            "ModuleInfo",
            "ShipLocker"
        };

        #endregion

        #region Private Methods

        private void SetLogMonitorState(LogMonitorState newState)
        {
            var oldState = currentState;
            currentState = newState;
            LogMonitorStateChanged?.Invoke(this, new LogMonitorStateChangedEventArgs
            {
                PreviousState = oldState,
                NewState = newState
            });;

            _logger.LogInformation("LogMonitor State change: {0} -> {1}", oldState, newState);
        }

        private void InitializeWatchers(string path)
        {
            DirectoryInfo logDirectory = GetJournalFolder(path);

            journalWatcher = new FileSystemWatcher(logDirectory.FullName, "Journal.*.??.log")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size |
                                NotifyFilters.FileName | NotifyFilters.CreationTime
            };
            journalWatcher.Changed += LogChangedEvent;
            journalWatcher.Created += LogCreatedEvent;

            statusWatcher = new FileSystemWatcher(logDirectory.FullName, "Status.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            statusWatcher.Changed += StatusUpdateEvent;
        }

        private DirectoryInfo GetJournalFolder(string path = "")
        {
            DirectoryInfo logDirectory;

            if (String.IsNullOrWhiteSpace(path))
            {
                path = _settings.JournalFolder;
            }

            if (!String.IsNullOrWhiteSpace(path))
            {
                if (Directory.Exists(path))
                {
                    logDirectory = new DirectoryInfo(path);
                }
                else
                {
                    //throw new DirectoryNotFoundException($"Directory '{path}' does not exist.");
                    //Don't throw, not handling that right now. Just set to current folder.
                    logDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string defaultJournalPath = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) 
                    + "/.steam/debian-installation/steamapps/compatdata/359320/pfx/drive_c/users/steamuser/Saved Games/Frontier Developments/Elite Dangerous"
                    : Path.Combine(GetSavedGamesPath(), @"Frontier Developments\Elite Dangerous");

                logDirectory =
                    Directory.Exists(defaultJournalPath)
                    ? new DirectoryInfo(defaultJournalPath)
                    : new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            }
            else
            {
                throw new NotImplementedException("Current OS Platform Not Supported.");
            }

            if (String.IsNullOrWhiteSpace(path))
            {
                _settings.JournalFolder = logDirectory.FullName;
            }

            return logDirectory;
        }

       
        private JournalEventArgs DeserializeToEventArgs(string eventType, string line)
        {
            
            var eventClass = journalTypes[eventType];
            MethodInfo journalRead = typeof(JournalReader).GetMethod(nameof(JournalReader.ObservatoryDeserializer));
            MethodInfo journalGeneric = journalRead.MakeGenericMethod(eventClass);
            object entry = journalGeneric.Invoke(null, new object[] { line });
            return new JournalEventArgs() { journalType = eventClass, journalEvent = entry };
        }

        private void DeserializeAndInvoke(string file, string line)
        {
            try
            {
                var eventType = JournalUtilities.GetEventType(line);
                if (!journalTypes.ContainsKey(eventType))
                {
                    eventType = "JournalBase";
                }

                var journalEvent = DeserializeToEventArgs(eventType, line);

                JournalEntry?.Invoke(this, journalEvent);

                // Files are only valid if realtime, otherwise they will be stale or empty.
                if (!currentState.HasFlag(LogMonitorState.Batch) && EventsWithAncillaryFile.Contains(eventType))
                {
                    HandleAncillaryFile(eventType);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"While processing log lines in {file}");
            }
        }

        private void HandleAncillaryFile(string eventType)
        {
            string filename = eventType == "ModuleInfo"
                ? "ModulesInfo.json" // Just FDev things
                : eventType + ".json";

            // I have no idea what order Elite writes these files or if they're already written
            // by the time the journal updates.
            // Brief sleep to ensure the content is updated before we read it.
            
            // Some files are still locked by another process after 50ms.
            // Retry every 50ms for 0.5 seconds before giving up.

            string fileContent = null;
            int retryCount = 0;
            
            while (fileContent == null && retryCount < 10)
            {
                System.Threading.Thread.Sleep(50);
                try
                {
                    using var fileStream = File.Open(journalWatcher.Path + Path.DirectorySeparatorChar + filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fileStream);
                    fileContent = reader.ReadToEnd();
                    var fileObject = DeserializeToEventArgs(eventType + "File", fileContent);
                    JournalEntry?.Invoke(this, fileObject);
                }
                catch
                {
                    retryCount++;
                }
            }
        }


        private void LogChangedEvent(object source, FileSystemEventArgs eventArgs)
        {
            var filename = Path.GetFileName(eventArgs.FullPath);
            var fileContent = ReadAllLines(eventArgs.FullPath).ToList();

            if (!currentLine.ContainsKey(eventArgs.FullPath))
            {
                currentLine.Add(eventArgs.FullPath, fileContent.Count - 1);
            }

            foreach (string line in fileContent.Skip(currentLine[eventArgs.FullPath]))
            {
                DeserializeAndInvoke(filename, line);
            }

            currentLine[eventArgs.FullPath] = fileContent.Count;
        }

        private IEnumerable<string> ReadAllLines(string path)
        {
            using (StreamReader file = new StreamReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                while (!file.EndOfStream)
                {
                    yield return file.ReadLine();
                }
            }
        }

        private async IAsyncEnumerable<string> ReadAllLinesAsync(string path)
        {
            var lines = new List<string>();
            using(Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (StreamReader file = new StreamReader(stream))
            {
                while (!file.EndOfStream)
                {
                    yield return await file.ReadLineAsync();
                }
            }
        }

        private void LogCreatedEvent(object source, FileSystemEventArgs eventArgs)
        {
            currentLine.Add(eventArgs.FullPath, 0);
            LogChangedEvent(source, eventArgs);
        }

        private void StatusUpdateEvent(object source, FileSystemEventArgs eventArgs)
        {
            var handler = StatusUpdate;
            var statusLines = ReadAllLines(eventArgs.FullPath);
            if (statusLines.Any())
            {
                var status = JournalReader.ObservatoryDeserializer<Status>(statusLines.First());
                handler?.Invoke(this, new JournalEventArgs() { journalType = typeof(Status), journalEvent = status });
            }
        }

        /// <summary>
        /// Touches most recent journal file once every 250ms while LogMonitor is monitoring.
        /// Forces pending file writes to flush to disk and fires change events for new journal lines.
        /// </summary>
        private async void JournalPoke()
        {
            var journalFolder = GetJournalFolder();

            await System.Threading.Tasks.Task.Run(() => 
            { 
                while (IsMonitoring())
                {
                    var journals = GetJournalFilesOrdered(journalFolder);

                    if (journals.Any())
                    {
                        FileInfo fileToPoke = GetJournalFilesOrdered(journalFolder).Last();

                        using FileStream stream = fileToPoke.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        stream.Close();
                    }
                    System.Threading.Thread.Sleep(250);
                }
            });
        }

        private static string GetSavedGamesPath()
        {
            if (Environment.OSVersion.Version.Major < 6) throw new NotSupportedException();
            IntPtr pathPtr = IntPtr.Zero;
            try
            {
                Guid FolderSavedGames = new Guid("4C5C32FF-BB9D-43b0-B5B4-2D72E54EAAA4");
                SHGetKnownFolderPath(ref FolderSavedGames, 0, IntPtr.Zero, out pathPtr);
                return Marshal.PtrToStringUni(pathPtr);
            }
            finally
            {
                Marshal.FreeCoTaskMem(pathPtr);
            }
        }

        private IEnumerable<FileInfo> GetJournalFilesOrdered(DirectoryInfo journalFolder)
        {
            foreach (var file in journalFolder.GetFiles("Journal.*.??.log").OrderBy(f => f.LastWriteTime))
                yield return file;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHGetKnownFolderPath(ref Guid id, int flags, IntPtr token, out IntPtr path);

        #endregion
    }
}
