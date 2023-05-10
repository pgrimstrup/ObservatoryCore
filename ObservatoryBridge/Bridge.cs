using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json.Serialization;
using Observatory.Bridge.Events;
using Observatory.Framework;
using Observatory.Framework.Files;
using Observatory.Framework.Files.Journal;
using Observatory.Framework.Files.ParameterTypes;
using Observatory.Framework.Interfaces;

namespace Observatory.Bridge
{
    internal class Bridge : IObservatoryWorker
    {
        public static Bridge Instance { get; private set; } = null!;

        BridgeSettings _settings = new BridgeSettings();

        PluginUI _ui = null!;
        ConcurrentDictionary<Type, (object?, MethodInfo?)> _eventHandlers = new ConcurrentDictionary<Type, (object?, MethodInfo?)>();
        List<object> _batchReadEvents = new List<object>();

        internal IObservatoryCore Core = null!;
        internal ObservableCollection<object> Events = new ObservableCollection<object>();
        internal CurrentSystemData CurrentSystem = new CurrentSystemData(new FSDJump());
        internal CurrentShipData CurrentShip = new CurrentShipData();
        internal Rank CurrentRank = new Rank();

        public string Name => "Observatory Bridge";

        public string ShortName => "Bridge";

        public string Version => typeof(Bridge).Assembly.GetName().Version?.ToString() ?? "";

        public PluginUI PluginUI => _ui;

        object IObservatoryPlugin.Settings
        {
            get => _settings;
            set => _settings = (value as BridgeSettings) ?? _settings;
        }

        public BridgeSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        public Bridge()
        {
            Instance = this;

            // Create all the event handlers declared in the assembly
            foreach (var handlerType in GetType().Assembly.GetTypes())
                foreach (var interfaceType in handlerType.GetInterfaces())
                    if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IJournalEventHandler<>))
                    {
                        var journalType = interfaceType.GetGenericArguments()[0];
                        var instance = Activator.CreateInstance(handlerType);
                        var method = handlerType.GetMethod("HandleEvent");
                        _eventHandlers.TryAdd(journalType, new(instance, method));
                    }
        }

        public void Load(IObservatoryCore observatoryCore)
        {
            try
            {
                _ui = new PluginUI(Events);
                Core = observatoryCore;
            }
            catch (Exception ex)
            {
                Core.GetPluginErrorLogger(this).Invoke(ex, "While loading Bridge plugin");
            }
        }

        public void JournalEvent<TJournal>(TJournal journal) where TJournal : JournalBase
        {
            // Dynamic event handling. This method will try to find a class that implements
            // IJournalEventHandler<T>, where T is the type of journal entry to handle. If one
            // can be found, it is created. If one already exists, then it is used.
            // The HandleEvent method is then called on the object instance passing through the 
            // Journal entry. 
            // To handle additional Journal Types, simply create a new class that implements
            // IJournalEventHandler<> and it will be auto-discovered.
            if (!Settings.BridgeEnabled)
                return;

            // First check for an event handler class for the journal type
            (object? Instance, MethodInfo? Method) handler = _eventHandlers.GetOrAdd(journal.GetType(), key => {
                // All of the handler have been pre-loaded. If one hasn't been loaded, then add in a dummy value
                return new (null, null);
            });

            if(handler.Instance != null && handler.Method != null)
            {
                try
                {
                    // Found a handler, so call it to handle the event
                    Core.GetPluginErrorLogger(this).Invoke(null, $"Bridge Event: {journal.Event} ({journal.GetType().Name}) handled by {handler.Instance.GetType().Name}.{handler.Method.Name}");
                    handler.Method.Invoke(handler.Instance, new object[] { journal });
                }
                catch(Exception ex)
                {
                    Core.GetPluginErrorLogger(this).Invoke(ex, $"When calling {handler.Instance.GetType().Name}.{handler.Method.Name} for JournalEveny '{journal.GetType().Name}'");
                }
            }
        }

        public void StatusChange(Status status)
        {
            JournalEvent(status);
        }

        public void LogMonitorStateChanged(LogMonitorStateChangedEventArgs e)
        {
            if (!LogMonitorStateChangedEventArgs.IsBatchRead(e.PreviousState) && LogMonitorStateChangedEventArgs.IsBatchRead(e.NewState))
            {
                // Starting a batch read
                Core.ExecuteOnUIThread(() => {
                    Events.Clear();
                });
            }

            if (LogMonitorStateChangedEventArgs.IsBatchRead(e.PreviousState) && !LogMonitorStateChangedEventArgs.IsBatchRead(e.NewState))
            {
                CurrentShip.Status = 0;
                CurrentShip.Status2 = 0;

                // Finished a batch read
                Core.ExecuteOnUIThread(() => {
                    // Read from the lastFSD Jump entry 
                    Events.Clear();
                    for (int i = 0; i < _batchReadEvents.Count; i++)
                        Events.Add(_batchReadEvents[i]);
                    _batchReadEvents.Clear();
                });
            }
        }

        internal void LogEvent(BridgeLog log, BridgeSettings? options = null)
        {
            options ??= this.Settings;
            if (log.IsText)
            {
                if (Core.IsLogMonitorBatchReading)
                {
                    if(log.EventName == "LaunchSRV")
                    {
                        CurrentShip.Status &= ~StatusFlags.MainShip;
                        CurrentShip.Status |= StatusFlags.SRV;
                    }

                    if(log.EventName == "DockSRV")
                    {
                        CurrentShip.Status &= ~StatusFlags.SRV;
                        CurrentShip.Status |= StatusFlags.MainShip;
                    }

                    // Keep everythng after the last FSD Jump
                    if (log.EventName == "FSDJump")
                        _batchReadEvents.Clear();
                    _batchReadEvents.Add(log);
                }
                else
                    Core.ExecuteOnUIThread(() => {
                        Events.Add(log);
                    });
            }

            if (log.IsSpoken)
            {
                var e = new NotificationArgs {
                    Title = log.TitleSsml.ToString() ,
                    TitleSsml = log.TitleSsml.ToSsml(),
                    Detail = log.DetailSsml.ToString(),
                    DetailSsml = log.DetailSsml.ToSsml(),
                    Rendering = 0
                };

                if (options.UseHeraldVocalizer)
                    e.Rendering |= NotificationRendering.PluginNotifier;
                if (options.UseInternalVocalizer)
                    e.Rendering |= NotificationRendering.NativeVocal;

                if (String.IsNullOrEmpty(e.Title))
                {
                    // Empty titles are always suppressed
                    e.Suppression |= NotificationSuppression.Title;
                    log.IsTitleSpoken = false;
                }

                if (String.IsNullOrEmpty(e.Detail))
                {
                    // Empty details are always suppressed
                    e.Suppression |= NotificationSuppression.Detail;
                    log.IsDetailSpoken = false;
                }

                if(!options.AlwaysSpeakTitles && !log.IsTitleSpoken)
                {
                    e.Suppression |= NotificationSuppression.Title;
                }

                if (!log.IsDetailSpoken)
                {
                    e.Suppression |= NotificationSuppression.Detail;
                }

                Core.SendNotification(e);
            }
        }
    }
}