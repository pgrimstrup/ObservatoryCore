using System;
using Observatory.Framework.Files.Journal;
using Observatory.Framework.Files;
using Observatory.Framework;
using Observatory.Framework.Interfaces;
using Observatory.PluginManagement;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Observatory
{
    public class ObservatoryCore : IObservatoryCoreAsync
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


        public ObservatoryCore(
            IServiceProvider services, 
            ILogger<ObservatoryCore> logger, 
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

        public IEnumerable<IObservatoryPlugin> Initialize()
        {
            if (_pluginsInitialized)
                throw new InvalidOperationException("IObserverCore.Initializes cannot be called more than once");

            _logMonitor.JournalEntry += OnJournalEvent;
            _logMonitor.StatusUpdate += OnStatusUpdate;
            _logMonitor.LogMonitorStateChanged += OnLogMonitorStateChanged;

            // PluginManager needs Core to be created first (circular reference), so we can create PluginManager here
            _pluginManager = Services.GetRequiredService<PluginManager>();
            _pluginManager.LoadPlugins();

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
            var args = new NotificationArgs { Title = title, Detail = text, Rendering = NotificationRendering.All };
            return Task.Run(() => SendNotificationAsync(args)).GetAwaiter().GetResult();
        }

        public async Task<Guid> SendNotificationAsync(string title, string text)
        {
            var args = new NotificationArgs { Title = title, Detail = text };
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

            // Queue the message to the internal VoiceQueue 
            Guid id = Guid.NewGuid();
            if((args.Rendering & NotificationRendering.NativeVocal) != 0)
            {
                string title = (args.Suppression & NotificationSuppression.Title) == 0 ? GetSsml(args.Title, args.TitleSsml) : "";
                string detail = (args.Suppression & NotificationSuppression.Title) == 0 ? GetSsml(args.Detail, args.DetailSsml) : "";
                _voiceQueue.Add(new VoiceMessage { Id = id, Title = title, Detail = detail });
            }

            // Queue the message to the internal PopupQueue
            if ((args.Rendering & NotificationRendering.NativeVisual) != 0)
            {
                string title = "";
                string detail = "";

                if ((args.Suppression & NotificationSuppression.Title) == 0 && !String.IsNullOrEmpty(args.TitleSsml))
                    title = args.Title.TrimEnd(' ', '.');

                if ((args.Suppression & NotificationSuppression.Detail) == 0 && !String.IsNullOrEmpty(args.DetailSsml))
                    detail = args.Detail.TrimEnd(' ', '.');

                if (!String.IsNullOrEmpty(title) || !String.IsNullOrEmpty(detail))
                    _popupQueue.Add(new PopupMessage { Id = id, Title = title, Detail = detail, Timeout = args.Timeout });
            }

            if ((args.Rendering & NotificationRendering.PluginNotifier) != 0)
            {
                // Notify all plugins of the event
                await Parallel.ForEachAsync(_pluginManager.ActivePlugins, async (plugin, token) =>
                {
                    try
                    {
                        if (plugin is IObservatoryNotifierAsync notifierAsync)
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

            return id;

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

            if ((args.Rendering & NotificationRendering.NativeVocal) != 0)
            {
                string title = (args.Suppression & NotificationSuppression.Title) == 0 ? GetSsml(args.Title, args.TitleSsml) : "";
                string detail = (args.Suppression & NotificationSuppression.Title) == 0 ? GetSsml(args.Detail, args.DetailSsml) : "";
                _voiceQueue.Update(new VoiceMessage { Id = id, Title = title, Detail = detail });
            }

            if ((args.Rendering & NotificationRendering.NativeVisual) != 0)
            {
                string title = "";
                string detail = "";

                if ((args.Suppression & NotificationSuppression.Title) == 0 && !String.IsNullOrEmpty(args.TitleSsml))
                    title = args.Title.TrimEnd(' ', '.');

                if ((args.Suppression & NotificationSuppression.Detail) == 0 && !String.IsNullOrEmpty(args.DetailSsml))
                    detail = args.Detail.TrimEnd(' ', '.');

                if (!String.IsNullOrEmpty(title) || !String.IsNullOrEmpty(detail))
                    _popupQueue.Update(new PopupMessage { Id = id, Title = title, Detail = detail, Timeout = args.Timeout });
            }

            // Send the update message to all plugins that support the new Async interfaces
            if ((args.Rendering & NotificationRendering.PluginNotifier) != 0)
            {
                await Parallel.ForEachAsync(_pluginManager.ActivePlugins, async (plugin, token) => {
                    try
                    {
                        // Updates are only sent to IObservatoryNotifierAsync
                        if (plugin is IObservatoryNotifierAsync notifier)
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
                    if (plugin is IObservatoryNotifierAsync notifier)
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

        private string GetSsml(string text, string ssml)
        {
            if (String.IsNullOrEmpty(ssml) && String.IsNullOrEmpty(text))
                return null;

            if (String.IsNullOrEmpty(ssml))
            {
                var sb = new SsmlBuilder();
                sb.Append(text);
                ssml = sb.ToSsml();
            }

            return ssml.TrimEnd(' ', '.');
        }


        public Action<Exception, string> GetPluginErrorLogger(IObservatoryPlugin plugin)
        {
            return (ex, msg) => _logger.LogError(ex, msg);
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

                //Hacky removal of original empty object if one was used to populate columns
                if (worker.PluginUI.DataGrid.Count == 2)
                {
                    if (FirstRowIsAllNull(worker))
                        worker.PluginUI.DataGrid.RemoveAt(0);
                }
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
                var cleanEmptyRow = worker.PluginUI.DataGrid.Count == 1 && FirstRowIsAllNull(worker) && items.Count() > 0;
                foreach (var item in items)
                {
                    worker.PluginUI.DataGrid.Add(item);
                }
                if (cleanEmptyRow)
                    worker.PluginUI.DataGrid.RemoveAt(0);
            });
        }

        public void ClearGrid(IObservatoryWorker worker, object templateItem)
        {
            ExecuteOnUIThread(() => {
                worker.PluginUI.DataGrid.Add(templateItem);
                while (worker.PluginUI.DataGrid.Count > 1)
                    worker.PluginUI.DataGrid.RemoveAt(0);
            });
        }

        public void ExecuteOnUIThread(Action action)
        {
            _dispatcher.Run(action);
            //Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(action);
        }

        public LogMonitorState CurrentLogMonitorState
        {
            get => _logMonitor.CurrentState;
        }

        public bool IsLogMonitorBatchReading
        {
            get => LogMonitorStateChangedEventArgs.IsBatchRead(_logMonitor.CurrentState);
        }

        public string PluginStorageFolder
        {
            get
            {
                var context = new System.Diagnostics.StackFrame(1).GetMethod();

                string folderLocation = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                    + $"{Path.DirectorySeparatorChar}ObservatoryCore{Path.DirectorySeparatorChar}{context.DeclaringType.Assembly.GetName().Name}{Path.DirectorySeparatorChar}";

                if (!Directory.Exists(folderLocation))
                    Directory.CreateDirectory(folderLocation);

                return folderLocation;
            }
        }

        public HttpClient HttpClient => Services.GetRequiredService<HttpClient>();

        internal void Shutdown()
        {
            _pluginManager.Shutdown();
            _voiceQueue.Shutdown();
            _popupQueue.Shutdown();
        }

        private static bool FirstRowIsAllNull(IObservatoryWorker worker)
        {
            bool allNull = true;
            Type itemType = worker.PluginUI.DataGrid[0].GetType();
            foreach (var property in itemType.GetProperties())
            {
                if (property.GetValue(worker.PluginUI.DataGrid[0], null) != null)
                {
                    allNull = false;
                    break;
                }
            }

            return allNull;
        }

        public void OnJournalEvent(object sender, JournalEventArgs e)
        {
            foreach (var plugin in _pluginManager.ActivePlugins)
            {
                try
                {
                    (plugin as IObservatoryWorker)?.JournalEvent((JournalBase)e.journalEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Plugin {plugin.Name}, EventType {e.journalEvent} exception while handling journal event");
                }
            }
        }

        public void OnStatusUpdate(object sender, JournalEventArgs e)
        {
            foreach (var plugin in _pluginManager.ActivePlugins)
            {
                try
                {
                    (plugin as IObservatoryWorker)?.StatusChange((Status)e.journalEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Plugin {plugin.Name}, EventType {e.journalEvent} exception while handling status update");
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

    }
}
