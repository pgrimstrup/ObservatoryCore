using System;
using StarGazer.Framework;
using StarGazer.Framework.Interfaces;
using StarGazer.Plugins;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Observatory.Framework.Interfaces;
using Observatory.Framework.Files;
using Observatory.Framework;
using Observatory.Framework.Files.Journal;

namespace StarGazer
{
    public class StarGazerCore : IStarGazerCore
    {
        private PluginManager _pluginManager;
        private readonly ILogger _logger;
        private readonly ILogMonitor _logMonitor;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMainFormDispatcher _dispatcher;
        private readonly IAppSettings _settings;
        private readonly IVoiceNotificationQueue _voiceQueue;
        private readonly IVisualNotificationQueue _popupQueue;
        private bool _pluginsInitialized;


        public StarGazerCore(
            IServiceProvider services, 
            ILogger<StarGazerCore> logger, 
            ILogMonitor logMonitor, 
            IMainFormDispatcher dispatcher, 
            IVoiceNotificationQueue voiceQueue,
            IVisualNotificationQueue popupQueue,
            IAppSettings settings)
        {
            _serviceProvider = services;
            _logger = logger;
            _logMonitor = logMonitor;
            _dispatcher = dispatcher;
            _voiceQueue = voiceQueue;
            _popupQueue = popupQueue;
            _settings = settings;
        }

        public IServiceProvider Services => _serviceProvider;

        public IEnumerable<IObservatoryPlugin> ActivePlugins => _pluginManager.ActivePlugins;

        public async Task<IEnumerable<IObservatoryPlugin>> InitializeAsync()
        {
            if (_pluginsInitialized)
                throw new InvalidOperationException("IObserverCore.Initializes cannot be called more than once");

            LoadCoreSettings();

            _logMonitor.JournalEntry += OnJournalEvent;
            _logMonitor.StatusUpdate += OnStatusUpdate;
            _logMonitor.LogMonitorStateChanged += OnLogMonitorStateChanged;

            // PluginManager needs Core to be created first (circular reference), so we can create PluginManager here
            _pluginManager = Services.GetRequiredService<PluginManager>();
            await _pluginManager.LoadPluginsAsync();

            if (_settings.StartMonitor)
                _logMonitor.Start();

            // Enable notifications
            _pluginsInitialized = true;
            return _pluginManager.ActivePlugins;
        }

        public PluginManager PluginManager => _pluginManager;
        public string Version => System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();

        public Status GetStatus()
        {
            throw new NotImplementedException();
        }

        public Guid SendNotification(string title, string text)
        {
            var args = new VoiceNotificationArgs { Title = title, Detail = text, Rendering = NotificationRendering.All };
            return Task.Run(() => SendNotificationAsync(args)).GetAwaiter().GetResult();
        }

        public async Task<Guid> SendNotificationAsync(string title, string text)
        {
            var args = new VoiceNotificationArgs { Title = title, Detail = text };
            return await SendNotificationAsync(args);
        }

        public Guid SendNotification(NotificationArgs args)
        {
            return Task.Run(() => SendNotificationAsync(args)).GetAwaiter().GetResult();
        }

        public async Task<Guid> SendNotificationAsync(NotificationArgs args)
        {
            if (!_pluginsInitialized)
                return Guid.Empty;

            if (_logMonitor.CurrentState.HasFlag(LogMonitorState.Batch) || _logMonitor.CurrentState.HasFlag(LogMonitorState.PreRead))
                return Guid.Empty;

            // Queue the message to the internal VoiceQueue 
            var messageId = Guid.NewGuid();
            if((args.Rendering & NotificationRendering.NativeVocal) != 0 && _settings.InbuiltVoiceEnabled)
            {
                if (args is VoiceNotificationArgs voiceNotificationArgs)
                {
                    _voiceQueue.Add(voiceNotificationArgs);
                }
                else
                    _voiceQueue.Add(new VoiceNotificationArgs {
                        Id = messageId,
                        Title = args.Title,
                        TitleSsml = args.TitleSsml,
                        Detail = args.Detail,
                        DetailSsml = args.DetailSsml,
                        Rendering = args.Rendering,
                        Suppression = args.Suppression
                    });
            }

            // Queue the message to the internal PopupQueue
            if ((args.Rendering & NotificationRendering.NativeVisual) != 0 && _settings.InbuiltPopupsEnabled)
            {
                if (args is VisualNotificationArgs visualNotificationArgs)
                {
                    _popupQueue.Add(visualNotificationArgs);
                }
                else
                    _popupQueue.Add(new VisualNotificationArgs {
                        Id = messageId,
                        Title = args.Title,
                        Detail = args.Detail,
                        Timeout = args.Timeout,
                        Suppression = args.Suppression,
                        Rendering = args.Rendering
                    });
            }

            if ((args.Rendering & NotificationRendering.PluginNotifier) != 0)
            {
                // Notify all notifier plugins of the event
                await Parallel.ForEachAsync(_pluginManager.ActivePlugins, async (plugin, token) =>
                {
                    try
                    {
                        if (plugin is IStarGazerNotifier notifierAsync)
                        {
                            await notifierAsync.OnNotificationEventAsync(args);
                        }
                        else if (plugin is IObservatoryNotifier notifier)
                        {
                            notifier.OnNotificationEvent(args);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Plugin {plugin.Name} exception while sending notification");
                    }
                });
            }

            return messageId;

        }

        public void UpdateNotification(Guid id, NotificationArgs args)
        {
            if (!_pluginsInitialized)
                return;

            Task.Run(() => UpdateNotificationAsync(id, args)).GetAwaiter().GetResult();
        }

        public async Task UpdateNotificationAsync(Guid id, NotificationArgs args)
        {
            if (!_pluginsInitialized)
                return;

            if(args is VisualNotificationArgs visualNotificationArgs)
            {
                visualNotificationArgs.Id = id;
                _popupQueue.Update(visualNotificationArgs);
            }
            else if(args is VoiceNotificationArgs voiceNotificationArgs)
            {
                voiceNotificationArgs.Id = id;
                _voiceQueue.Update(voiceNotificationArgs);
            }
            else
            {
                if ((args.Rendering & NotificationRendering.NativeVocal) != 0)
                {
                    _voiceQueue.Update(new VoiceNotificationArgs {
                        Id = id,
                        Title = args.Title,
                        TitleSsml = args.TitleSsml,
                        Detail = args.Detail,
                        DetailSsml = args.DetailSsml,
                        Rendering = args.Rendering,
                        Suppression = args.Suppression
                    });
                }
                if ((args.Rendering & NotificationRendering.NativeVisual) != 0)
                {
                    _popupQueue.Update(new VisualNotificationArgs { 
                        Id = id,
                        Title = args.Title,
                        TitleSsml = args.TitleSsml,
                        Detail = args.Detail,
                        DetailSsml = args.DetailSsml,
                        Rendering = args.Rendering,
                        Suppression = args.Suppression
                    });
                }
            }

            // Send the update message to all plugins that support the new Async interfaces
            if ((args.Rendering & NotificationRendering.PluginNotifier) != 0)
            {
                await Parallel.ForEachAsync(_pluginManager.ActivePlugins, async (plugin, token) => {
                    try
                    {
                        // Updates are only sent to IObservatoryNotifierAsync
                        if (plugin is IStarGazerNotifier notifier)
                        {
                             await notifier.OnNotificationEventAsync(id, args);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Plugin {plugin.Name} exception while sending notification");
                    }
                });
            }
        }


        public void CancelNotification(Guid id)
        {
            Task.Run(() => CancelNotificationAsync(id)).GetAwaiter().GetResult();
        }

        public async Task CancelNotificationAsync(Guid id)
        {
            _voiceQueue.Cancel(id);
            _popupQueue.Cancel(id);

            await Parallel.ForEachAsync(_pluginManager.ActivePlugins, async (plugin, token) => {
                try
                {
                    // Cancellations are only sent to IObservatoryNotifierAsync
                    if (plugin is IStarGazerNotifier notifier)
                    {
                        await notifier.OnNotificationCancelledAsync(id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Plugin {plugin.Name} exception while sending notification");
                }
            });
        }

        public Action<Exception, string> GetPluginErrorLogger(IObservatoryPlugin plugin)
        {
            return (ex, msg) => {
                if (ex == null)
                    _logger.LogInformation(msg);
                else
                    _logger.LogError(ex, msg);
            };
        }

        /// <summary>
        /// Adds an item to the datagrid on UI thread to ensure visual update.
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="item"></param>
        public void AddGridItem(IObservatoryWorker worker, object item)
        {
            ExecuteOnUIThread(() => {
                worker.PluginUI.DataGrid.Add(item);
            });
        }

        /// <summary>
        /// Adds multiple items to the datagrid on UI thread to ensure visual update.
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="items"></param>
        public void AddGridItems(IObservatoryWorker worker, IEnumerable<object> items)
        {
            ExecuteOnUIThread(() => {
                foreach (var item in items)
                {
                    worker.PluginUI.DataGrid.Add(item);
                }
            });
        }

        public void ClearGrid(IObservatoryWorker worker, object templateItem)
        {
            ExecuteOnUIThread(() => {
                worker.PluginUI.DataGrid.Clear();
            });
        }

        public void ExecuteOnUIThread(Action action)
        {
            _dispatcher.Run(action);
        }

        public LogMonitorState CurrentLogMonitorState
        {
            get => _logMonitor.CurrentState;
        }

        public bool IsLogMonitorBatchReading
        {
            get => LogMonitorStateChangedEventArgs.IsBatchRead(_logMonitor.CurrentState);
        }


        public HttpClient HttpClient => Services.GetRequiredService<HttpClient>();

        internal void Shutdown()
        {
            _pluginManager.Shutdown();
            _voiceQueue.Shutdown();
            _popupQueue.Shutdown();

            SaveCoreSettings();
        }


        public void OnJournalEvent(object sender, JournalEventArgs e)
        {
            JournalBase journal = (JournalBase)e.journalEvent;
            _logger.LogDebug($"{journal.Timestamp:HH:mm:ss.fff}: {journal.Event}");

            foreach (var plugin in _pluginManager.ActivePlugins.ToArray())
            {
                try
                {
                    if (plugin is IStarGazerWorker sgWorker)
                    {
                        sgWorker.JournalEventAsync(journal);
                    }
                    else if(plugin is IObservatoryWorker obWorker)
                    {
                        obWorker.JournalEvent(journal);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Plugin {plugin.Name}, EventType {e.journalEvent} exception while handling journal event");
                }
            }
        }

        string _lastStatus = "";

        public void OnStatusUpdate(object sender, JournalEventArgs e)
        {
            Status status = (Status)e.journalEvent;
            if (status.Json == _lastStatus)
                return;

            _lastStatus = status.Json;
            _logger.LogDebug($"{status.Timestamp:HH:mm:ss.fff}: {status.Event}");

            foreach (var plugin in _pluginManager.ActivePlugins.ToArray())
            {
                try
                {
                    if (plugin is IStarGazerWorker sgWorker)
                    {
                        sgWorker.StatusChangeAsync(status);
                    }
                    else if (plugin is IObservatoryWorker obWorker)
                    {
                        obWorker.StatusChange(status);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Plugin {plugin.Name}, EventType {status.Event} exception while handling status update");
                }
            }
        }

        internal void OnLogMonitorStateChanged(object sender, LogMonitorStateChangedEventArgs e)
        {
            foreach (var plugin in _pluginManager.ActivePlugins)
            {
                try
                {
                    (plugin as IObservatoryWorker)?.LogMonitorStateChanged(e);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Plugin {plugin.Name}  exception while handling state change");
                }
            }
        }

        public Task<Status> GetStatusAsync()
        {
            return Task.FromResult(this.GetStatus());
        }

        public string PluginStorageFolder
        {
            get
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Star Gazer", "Data");
                if(!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        } 

        public string CoreFolder
        {
            get
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Star Gazer");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }

        public void SavePluginSettings(IObservatoryPlugin plugin)
        {
            if (plugin.Settings != null)
            {
                string path = Path.Combine(CoreFolder, $"{plugin.ShortName.ToLower()}.settings");
                string json = JsonSerializer.Serialize(plugin.Settings, CoreExtensions.SerializerOptions);
                File.WriteAllText(path, json);
            }
        }

        public void LoadPluginSettings(IObservatoryPlugin plugin)
        {
            string path = Path.Combine(CoreFolder, $"{plugin.ShortName.ToLower()}.settings");
            if(File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var settings = JsonSerializer.Deserialize(json, plugin.Settings.GetType(), CoreExtensions.SerializerOptions);
                if (settings != null)
                    plugin.Settings = settings;
            }
        }

        public void LoadCoreSettings()
        {
            string path = Path.Combine(CoreFolder, "core.settings");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize(json, _settings.GetType(), CoreExtensions.SerializerOptions);
                foreach (var property in _settings.GetType().GetProperties().Where(p => p.CanRead && p.CanWrite))
                    property.SetValue(_settings, property.GetValue(loaded));
            }
        }

        public void SaveCoreSettings()
        {
            string path = Path.Combine(CoreFolder, "core.settings");
            string json = JsonSerializer.Serialize(_settings, CoreExtensions.SerializerOptions);
            File.WriteAllText(path, json);
        }
    }
}
