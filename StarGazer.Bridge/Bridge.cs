using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using Observatory.Framework;
using Observatory.Framework.Files;
using Observatory.Framework.Files.Journal;
using Observatory.Framework.Files.ParameterTypes;
using Observatory.Framework.Interfaces;
using StarGazer.Bridge.Events;

namespace StarGazer.Bridge
{
    internal class Bridge : IObservatoryWorker
    {
        public static Bridge Instance { get; private set; } = null!;

        BridgeSettings _settings = new BridgeSettings();

        PluginUI _ui = null!;
        ConcurrentDictionary<Type, (object?, MethodInfo?)> _eventHandlers = new ConcurrentDictionary<Type, (object?, MethodInfo?)>();
        List<object> _batchReadEvents = new List<object>();

        internal IObservatoryCore Core = null!;
        internal CurrentGameState GameState = new CurrentGameState();
        internal Rank CurrentRank = new Rank();

        public string Name => "StarGazer Bridge";

        public string ShortName => "Bridge";

        public string Version => typeof(Bridge).Assembly.GetName().Version?.ToString(3) ?? "";

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

        public IEnumerable<BridgeLog> Logs
        {
            get
            {
                if (Core.IsLogMonitorBatchReading)
                    return _batchReadEvents.OfType<BridgeLog>();
                else
                    return PluginUI.DataGrid.OfType<BridgeLog>();
            }
        }

        public void Load(IObservatoryCore observatoryCore)
        {
            try
            {
                _ui = new PluginUI(new ObservableCollection<object>());
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

            // In all cases, GameState gets a preview of the journal
            GameState.Assign(journal);

            if (handler.Instance != null && handler.Method != null)
            {
                try
                {
                    // Found a handler, so call it to handle the event
                    handler.Method.Invoke(handler.Instance, new object[] { journal });
                }
                catch(Exception ex)
                {
                    Core.GetPluginErrorLogger(this).Invoke(ex, $"When calling {handler.Instance.GetType().Name}.{handler.Method.Name} for JournalEvent '{journal.GetType().Name}'");
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
                GameState.Status = 0;
                GameState.Status2 = 0;

                // Starting a batch read
                Core.ClearGrid(this, null);
            }

            if (LogMonitorStateChangedEventArgs.IsBatchRead(e.PreviousState) && !LogMonitorStateChangedEventArgs.IsBatchRead(e.NewState))
            {
                GameState.Status = 0;
                GameState.Status2 = 0;

                // Finished a batch read
                Core.ClearGrid(this, null);
                Core.AddGridItems(this, _batchReadEvents);
                _batchReadEvents.Clear();
            }
        }

        internal void LogEvent(BridgeLog log, BridgeSettings? options = null)
        {
            options ??= this.Settings;
            if (Core.IsLogMonitorBatchReading)
            {
                if (log.EventName == nameof(LaunchSRV))
                {
                    GameState.Status &= ~StatusFlags.MainShip;
                    GameState.Status |= StatusFlags.SRV;
                }

                if (log.EventName == nameof(DockSRV))
                {
                    GameState.Status &= ~StatusFlags.SRV;
                    GameState.Status |= StatusFlags.MainShip;
                }

                if (log.EventName == nameof(CarrierJump) || log.EventName == nameof(FSDJump) || log.EventName == nameof(Location))
                {
                    // find the last CarrierJumpRequest and keep it
                    var lastRequest = _batchReadEvents.OfType<BridgeLog>().LastOrDefault(b => b.EventName == nameof(CarrierJumpRequest) || b.EventName == nameof(StartJump));
                    _batchReadEvents.Clear();
                    if (lastRequest != null)
                        _batchReadEvents.Add(lastRequest);
                }

                if(log.IsText)
                    _batchReadEvents.Add(log);
            }
            else if(log.IsText)
                Core.AddGridItem(this, log);

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

                if (String.IsNullOrWhiteSpace(e.Title) && String.IsNullOrWhiteSpace(e.TitleSsml))
                {
                    // Empty titles are always suppressed
                    e.Suppression |= NotificationSuppression.Title;
                    log.IsTitleSpoken = false;
                }

                if (String.IsNullOrWhiteSpace(e.Detail) && String.IsNullOrWhiteSpace(e.DetailSsml))
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

        internal void ResetLogEntries()
        {
            void Reset(IList<object> logs)
            {
                var lastRequest = logs
                    .OfType<BridgeLog>()
                    .LastOrDefault(e => e.EventName == nameof(CarrierJumpRequest) || e.EventName == nameof(StartJump));

                if(lastRequest != null)
                {
                    int index = logs.IndexOf(lastRequest);
                    for (int i = 0; i < index; i++)
                        logs.RemoveAt(0);
                }
            }

            if (Core.IsLogMonitorBatchReading)
            {
                Reset(_batchReadEvents);
            }
            else
            {
                Bridge.Instance.Core.ExecuteOnUIThread(() => {
                    // Remove all entries up to the last CarrierJumpRequest or StartJump
                    Reset(Bridge.Instance.PluginUI.DataGrid);
                });
            }
        }
    }
}