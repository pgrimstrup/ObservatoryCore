using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace StarGazer.UI.Services
{
    public class FileLoggerOptions
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public LogLevel LogLevel { get; set; }
        public bool DailyRollover { get; set; }
        public int RolloverCount { get; set; }

        public FileLoggerOptions()
        {
            Name = "";
            FileName = Process.GetCurrentProcess().ProcessName + ".log";
        }
    }

    internal class FileLogger : ILogger
    {
        FileLoggerWriter _writer;
        FileLoggerOptions _options = new FileLoggerOptions();

        public FileLogger(Action<FileLoggerOptions> config)
        {
            config?.Invoke(_options);
            _writer = FileLoggerWriter.Create(_options);
        }

        public IDisposable? BeginScope<TState>(TState state) 
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _options.LogLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                _writer.Log(logLevel, eventId, state, exception, formatter);
            }
        }
    }

    internal class FileLoggerWriter
    {
        static ConcurrentDictionary<string, FileLoggerWriter> _writers = new ConcurrentDictionary<string, FileLoggerWriter>();

        public static FileLoggerWriter Create(FileLoggerOptions options)
        {
            return _writers.GetOrAdd(options.FileName, key => {
                return new FileLoggerWriter(options);
            });
        }

        FileLoggerOptions _options;
        Thread _thread;
        DateTime _lastDate;
        BlockingCollection<LogMessage> _messages = new BlockingCollection<LogMessage>();
        string _targetFile;
        bool _firstEntry = true;

        public FileLoggerWriter(FileLoggerOptions options)
        {
            _options = options;
            _lastDate = DateTime.Today;
            _targetFile = options.FileName;
            if (options.DailyRollover)
                RolloverDateFile();

            _thread = new Thread(Run);
            _thread.IsBackground = true;
            _thread.Start();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var log = new LogMessage {
                Timestamp = DateTime.Now,
                EventId = eventId,
                LogLevel = logLevel,
                Message = formatter(state, exception)
            };

            if(exception != null)
            {
                log.Message += $": {exception}";
            }

            _messages.Add(log);
        }

        public void Run()
        {
            while (true)
            {
                if(_messages.TryTake(out var log, 100))
                {
                    try
                    {
                        if (_lastDate != DateTime.Today)
                            RolloverDateFile();
                        else if (_firstEntry && _options.RolloverCount > 0)
                            RolloverNumberFile();

                        CreateDirectory();

                        string line = $"{log.Timestamp:HH:mm:ss:fff} - {log.LogLevel.ToString().ToUpper()} - {log.Message}{Environment.NewLine}";
                        File.AppendAllText(_targetFile, line);
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
            }
        }

        private void RolloverDateFile()
        {
            string folder = Path.GetDirectoryName(_options.FileName)!;
            string file = Path.GetFileNameWithoutExtension(_options.FileName);
            string ext = Path.GetExtension(_options.FileName);

            _lastDate = DateTime.Today;
            _targetFile = Path.Combine(folder, $"{file}_{_lastDate:yyyy-MM-dd}{ext}");
        }

        private void RolloverNumberFile()
        {
            string folder = Path.GetDirectoryName(_targetFile)!;
            string file = Path.GetFileNameWithoutExtension(_targetFile);
            string ext = Path.GetExtension(_targetFile);

            for (int i = _options.RolloverCount; i > 1; i--)
            {
                var rolloverFrom = Path.Combine(folder, $"{file}_{_lastDate:yyyy-MM-dd}.{i - 1}{ext}");
                if (i == 1)
                    rolloverFrom = _targetFile;

                var rolloverTo = Path.Combine(folder, $"{file}_{_lastDate:yyyy-MM-dd}.{i}{ext}");
                if (File.Exists(rolloverTo))
                    File.Delete(rolloverTo);

                if (File.Exists(rolloverFrom))
                    File.Move(rolloverFrom, rolloverTo, true);
            }
        }

        private void CreateDirectory()
        {
            if(!String.IsNullOrWhiteSpace(_targetFile))
                if (!Directory.Exists(Path.GetDirectoryName(_targetFile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(_targetFile)!);
        }
    }

    internal class LogMessage
    {
        public DateTime Timestamp { get; set; }
        public EventId EventId { get; set; }
        public LogLevel LogLevel { get; set; }
        public string Message { get; set; } = null!;
    }
}
