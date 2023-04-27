using Observatory.Framework;
using Observatory.Framework.Files;
using Observatory.Framework.Files.Journal;
using Observatory.Framework.Interfaces;
using Observatory.NativeNotification;
using System;
using System.IO;
using System.Text.Json;

namespace Observatory.PluginManagement
{
    public class PluginCore : IObservatoryCore
    {

        private readonly NativeVoice _nativeVoice;
        private readonly NativePopup _nativePopup;
        private readonly PluginManager _pluginManager;


        public PluginCore()
        {
            _nativeVoice = new();
            _nativePopup = new();
            _pluginManager = new PluginManager(this);
        }

        internal void InitializePlugins()
        {
            _pluginManager.LoadPlugins();

            var logMonitor = LogMonitor.GetInstance;
            logMonitor.JournalEntry += OnJournalEvent;
            logMonitor.StatusUpdate += OnStatusUpdate;
            logMonitor.LogMonitorStateChanged += OnLogMonitorStateChanged;

            _pluginManager.LoadPluginSettings();

            // Enable notifications
            this.Notification += OnNotificationEvent;
        }

        public PluginManager PluginManager => _pluginManager;
        public string Version => System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();

        public Action<Exception, String> GetPluginErrorLogger(IObservatoryPlugin plugin)
        {
            return (ex, context) =>
            {
                ObservatoryCore.LogError(ex, $"from plugin {plugin.ShortName} {context}");
            };
        }

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
            var guid = Guid.Empty;

            if (!IsLogMonitorBatchReading)
            {
                if (notificationArgs.Rendering.HasFlag(NotificationRendering.PluginNotifier))
                {
                    var handler = Notification;
                    handler?.Invoke(this, notificationArgs);
                }

                if (Properties.Core.Default.NativeNotify && notificationArgs.Rendering.HasFlag(NotificationRendering.NativeVisual))
                {
                    guid = _nativePopup.InvokeNativeNotification(notificationArgs);
                }

                if (Properties.Core.Default.VoiceNotify && notificationArgs.Rendering.HasFlag(NotificationRendering.NativeVocal))
                {
                    _nativeVoice.EnqueueAndAnnounce(notificationArgs);
                }
            }

            return guid;
        }

        public void CancelNotification(Guid id)
        {
            _nativePopup.CloseNotification(id);
        }

        public void UpdateNotification(Guid id, NotificationArgs notificationArgs)
        {
            if (!IsLogMonitorBatchReading)
            {

                if (notificationArgs.Rendering.HasFlag(NotificationRendering.PluginNotifier))
                {
                    var handler = Notification;
                    handler?.Invoke(this, notificationArgs);
                }

                if (notificationArgs.Rendering.HasFlag(NotificationRendering.NativeVisual))
                    _nativePopup.UpdateNotification(id, notificationArgs);

                if (Properties.Core.Default.VoiceNotify && notificationArgs.Rendering.HasFlag(NotificationRendering.NativeVocal))
                {
                    _nativeVoice.EnqueueAndAnnounce(notificationArgs);
                }
            }
        }

        /// <summary>
        /// Adds an item to the datagrid on UI thread to ensure visual update.
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="item"></param>
        public void AddGridItem(IObservatoryWorker worker, object item)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
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
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
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
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                worker.PluginUI.DataGrid.Add(templateItem);
                while (worker.PluginUI.DataGrid.Count > 1)
                    worker.PluginUI.DataGrid.RemoveAt(0);
            });
        }

        public void ExecuteOnUIThread(Action action)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(action);
        }

        public System.Net.Http.HttpClient HttpClient
        {
            get => Observatory.HttpClient.Client;
        }

        public LogMonitorState CurrentLogMonitorState
        {
            get => LogMonitor.GetInstance.CurrentState;
        }

        public bool IsLogMonitorBatchReading
        {
            get => LogMonitorStateChangedEventArgs.IsBatchRead(LogMonitor.GetInstance.CurrentState);
        }

        public event EventHandler<NotificationArgs> Notification;

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

        internal void Shutdown()
        {
            _nativePopup.CloseAll();
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

        public void OnJournalEvent(object source, JournalEventArgs e)
        {
            foreach (var plugin in _pluginManager.Plugins.Where(p => p.Instance != null && p.Error == null))
            {
                try
                {
                    (plugin.Instance as IObservatoryWorker)?.JournalEvent((JournalBase)e.journalEvent);
                }
                catch (PluginException ex)
                {
                    //RecordError(ex);
                }
                catch (Exception ex)
                {
                    //RecordError(ex, worker.Name, journalEventArgs.journalType.Name, ((JournalBase)journalEventArgs.journalEvent).Json);
                }
            }
        }

        public void OnStatusUpdate(object sourece, JournalEventArgs e)
        {
            foreach (var plugin in _pluginManager.Plugins.Where(p => p.Instance != null && p.Error == null))
            {
                try
                {
                    (plugin.Instance as IObservatoryWorker)?.StatusChange((Status)e.journalEvent);
                }
                catch (PluginException ex)
                {
                    //RecordError(ex);
                }
                catch (Exception ex)
                {
                    //RecordError(ex, worker.Name, journalEventArgs.journalType.Name, ((JournalBase)journalEventArgs.journalEvent).Json);
                }
            }
        }

        internal void OnLogMonitorStateChanged(object sender, LogMonitorStateChangedEventArgs e)
        {
            foreach (var plugin in _pluginManager.Plugins.Where(p => p.Instance != null && p.Error == null))
            {
                try
                {
                    (plugin.Instance as IObservatoryWorker)?.LogMonitorStateChanged(e);
                }
                catch (Exception ex)
                {
                    //RecordError(ex, worker.Name, "LogMonitorStateChanged event", ex.StackTrace);
                }
            }
        }

        public void OnNotificationEvent(object source, NotificationArgs e)
        {
            foreach (var plugin in _pluginManager.Plugins.Where(p => p.Instance != null && p.Error == null))
            {
                try
                {
                    (plugin.Instance as IObservatoryNotifier)?.OnNotificationEvent(e);
                }
                catch (PluginException ex)
                {
                    //RecordError(ex);
                }
                catch (Exception ex)
                {
                    //RecordError(ex, notifier.Name, notificationArgs.Title, notificationArgs.Detail);
                }
            }
        }

    }
}
