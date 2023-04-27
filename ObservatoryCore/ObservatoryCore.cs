using System;
using Observatory.Framework.Files.Journal;
using Observatory.Framework.Files;
using Observatory.Framework;
using Observatory.Framework.Interfaces;
using Observatory.PluginManagement;
using Microsoft.Extensions.Logging;

namespace Observatory
{
    public class ObservatoryCore : IObservatoryCore2
    {
        private PluginManager _pluginManager;
        private readonly ILogger _logger;
        private readonly ILogMonitor _logMonitor;
        private readonly IServiceProvider _serviceProvider;
        private bool _pluginsInitialized;


        public ObservatoryCore(IServiceProvider services, ILogger<ObservatoryCore> logger, ILogMonitor logMonitor)
        {
            _serviceProvider = services;
            _logger = logger;
            _logMonitor = logMonitor;
        }

        public T GetService<T>()
        {
            return (T)_serviceProvider.GetService(typeof(T));
        }

        public void Initialize()
        {
            if (_pluginsInitialized)
                throw new InvalidOperationException("IObserverCore.InitializePlugins cannot be called more than once");

            _pluginManager = GetService<PluginManager>();
            _pluginManager.LoadPlugins();

            _logMonitor.JournalEntry += OnJournalEvent;
            _logMonitor.StatusUpdate += OnStatusUpdate;
            _logMonitor.LogMonitorStateChanged += OnLogMonitorStateChanged;

            _pluginManager.LoadPluginSettings();

            // Enable notifications
            _pluginsInitialized = true;
        }

        public PluginManager PluginManager => _pluginManager;
        public string Version => System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();

        public Status GetStatus()
        {
            throw new NotImplementedException();
        }

        public Guid SendNotification(string title, string text)
        {
            return SendNotification(new NotificationArgs() { Title = title, Detail = text });
        }

        public Guid SendNotification(NotificationArgs notificationArgs)
        {
            if (!_pluginsInitialized)
                return Guid.Empty;

            Guid id = Guid.NewGuid();
            foreach (var plugin in _pluginManager.ActivePlugins)
            {
                try
                {
                    if(plugin is IInbuiltNotifier inbuilt)
                    {
                        if ((inbuilt.Filter & notificationArgs.Rendering) != 0)
                            inbuilt.OnNotificationEvent(id, notificationArgs);
                    }
                    else if(plugin is IObservatoryNotifier notifier)
                    {
                        notifier.OnNotificationEvent(notificationArgs);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Plugin {plugin.Name} exception while sending notification");
                }
            }

            return id;
        }

        public void UpdateNotification(Guid id, NotificationArgs notificationArgs)
        {
            if (!_pluginsInitialized)
                return;

            foreach (var plugin in _pluginManager.ActivePlugins)
            {
                try
                {
                    // Updates are only sent to InbuiltNotifiers
                    if (plugin is IInbuiltNotifier inbuilt)
                    {
                        if ((inbuilt.Filter & notificationArgs.Rendering) != 0)
                            inbuilt.OnNotificationEvent(id, notificationArgs);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Plugin {plugin.Name} exception while sending notification");
                }
            }
        }

        public void CancelNotification(Guid id)
        {
            foreach (var plugin in _pluginManager.ActivePlugins)
            {
                try
                {
                    (plugin as IInbuiltNotifier)?.OnNotificationCancelled(id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Plugin {plugin.Name} exception while cancelling notification");
                }
            }
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

        public HttpClient HttpClient => GetService<HttpClient>();

        internal void Shutdown()
        {
            _pluginManager.Shutdown();
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
    }
}
