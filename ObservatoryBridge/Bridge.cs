using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Observatory.Framework;
using Observatory.Framework.Files;
using Observatory.Framework.Files.Journal;
using Observatory.Framework.Files.ParameterTypes;
using Observatory.Framework.Interfaces;

namespace Observatory.Bridge
{
    public class Bridge : IObservatoryWorker
    {
        internal static Bridge Instance { get; private set; }

        BridgeSettings _settings = new BridgeSettings();
        ObservableCollection<object> _events = new ObservableCollection<object>();

        IObservatoryCore _core = null!;
        PluginUI _ui = null!;
        List<string> _scoopableStars = new List<string> { "K", "G", "B", "F", "O", "A", "M" };
        LogMonitorState _logState;

        internal Rank? CurrentRank;
        internal Status? CurrentStatus;
        internal CurrentSystemData CurrentSystem = new CurrentSystemData(new FSDJump());

        public string Name => "Observatory Bridge";

        public string ShortName => "Bridge";

        public string Version => typeof(Bridge).Assembly.GetName().Version.ToString();

        public PluginUI PluginUI => _ui;

        public object Settings
        {
            get => _settings;
            set => _settings = (value as BridgeSettings) ?? _settings;
        }

        internal BridgeSettings Options { get => (BridgeSettings)_settings; }

        public Bridge()
        {
            Instance = this;
        }

        public void Load(IObservatoryCore observatoryCore)
        {
            try
            {
                _ui = new PluginUI(_events);
                _core = observatoryCore;
            }
            catch (Exception ex)
            {
                ex.LogException();
            }
        }

        public void JournalEvent<TJournal>(TJournal journal) where TJournal : JournalBase
        {
            try
            {
                if (!Options.BridgeEnabled)
                    return;

                var methodName = $"Do{journal.GetType().Name}";
                var method = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
                if (method == null)
                {
                    ErrorLog.LogInfo($"INFO: JournalEvent: {journal.Event}, JournalType: {journal.GetType().Name}, Method {methodName} not found.");
                    return;
                }

                ErrorLog.LogInfo($"DBUG: JournalEvent: {journal.Event}, JournalType: {journal.GetType().Name}, Method {methodName} found.");
                method.Invoke(this, new object[] { journal });
            }
            catch (Exception ex)
            {
                ex.LogException();
            }
        }

        public void StatusChange(Status status)
        {
            if (CurrentStatus != null)
            {
                if (HasChanged(StatusFlags.Masslock, status))
                    DoMasslock(status);

                if (HasChanged(StatusFlags.LandingGear, status))
                    DoLandingGear(status);
            }
            CurrentStatus = status;
        }

        public void LogMonitorStateChanged(LogMonitorStateChangedEventArgs e)
        {
            _logState = e.NewState;
        }

        internal void LogEvent(BridgeLog log)
        {
            if (log.IsText)
            {
                _core.ExecuteOnUIThread(() => {
                    _events.Add(log);
                });
            }

            if (log.IsSpoken && _logState == LogMonitorState.Realtime)
            {
                var e = new NotificationArgs {
                    Title = log.TitleSsml.ToString() ,
                    TitleSsml = log.TitleSsml.ToSsml(),
                    Detail = log.DetailSsml.ToString(),
                    DetailSsml = log.DetailSsml.ToSsml(),
                    Rendering = 0
                };

                if (Options.UseHeraldVocalizer)
                    e.Rendering |= NotificationRendering.PluginNotifier;
                if (Options.UseInternalVocalizer)
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

                if(!Options.AlwaysSpeakTitles && !log.IsTitleSpoken)
                {
                    e.Suppression |= NotificationSuppression.Detail;
                }

                if (!log.IsDetailSpoken)
                {
                    e.Suppression |= NotificationSuppression.Detail;
                }

                ErrorLog.LogInfo($"SSML Rendering:\r\n    Title({log.IsTitleSpoken}) = {e.TitleSsml}\r\n    Detail = {e.DetailSsml}");
                _core.SendNotification(e);
            }
        }


        private bool HasChanged(StatusFlags flag, Status newstatus)
        {
            return (CurrentStatus.Flags & flag) != (newstatus.Flags & flag);
        }

        private string GetBodyName(string name)
        {
            if (CurrentSystem.SystemName == null || name.Length < CurrentSystem.SystemName.Length || !name.StartsWith(CurrentSystem.SystemName, StringComparison.OrdinalIgnoreCase))
                return name;

            // Single star system, primary star name is the same as the system name
            if (name.Equals(CurrentSystem.SystemName, StringComparison.OrdinalIgnoreCase))
                return "A";

            return name.Substring(CurrentSystem.SystemName.Length).Trim();
        }

        private void DoMasslock(Status newstatus)
        {
            var log = new BridgeLog(this);
            log.SpokenOnly();

            if (newstatus.Flags.HasFlag(StatusFlags.Masslock))
            {
                log.TitleSsml.Append("Flight Operations");
                log.DetailSsml.Append("Left mass lock, FSD available");
            }
            else
            {
                log.TitleSsml.Append("Flight Operations");
                log.DetailSsml.Append("Mass lock, FSD unavailable");
            }
            LogEvent(log);
        }

        private void DoLandingGear(Status newstatus)
        {
            var log = new BridgeLog(this);
            log.SpokenOnly();

            if (newstatus.Flags.HasFlag(StatusFlags.LandingGear))
            {
                log.TitleSsml.Append("Flight Operations");
                log.DetailSsml.Append("Landing gear down");
            }
            else
            {
                log.TitleSsml.Append("Flight Operations");
                log.DetailSsml.Append("Landing gear up");
            }
            LogEvent(log);
        }

        public void DoRank(Rank journal)
        {
            // Record current rank so we can detect promotions
            CurrentRank = journal;
        }

        public void DoPromotion(Promotion journal)
        {
            if (CurrentRank == null)
            {
                CurrentRank = journal;
                return;
            }

            List<string> promotions = new List<string>();
            if (journal.Empire > Observatory.Framework.Files.ParameterTypes.RankEmpire.None && journal.Empire > CurrentRank.Empire)
                promotions.Add($"Empire rank {journal.Empire}");
            if (journal.Federation > Observatory.Framework.Files.ParameterTypes.RankFederation.None && journal.Federation > CurrentRank.Federation)
                promotions.Add($"Federation rank {journal.Federation}");

            if (journal.Trade > CurrentRank.Trade)
                promotions.Add($"Trade rank {journal.Trade}");
            if (journal.Explore > CurrentRank.Explore)
                promotions.Add($"Exploration rank {journal.Explore}");
            if (journal.Exobiologist > CurrentRank.Exobiologist)
                promotions.Add($"Exobiologist rank {journal.Exobiologist}");
            if (journal.Combat > CurrentRank.Combat)
                promotions.Add($"Combat rank {journal.Combat}");
            if (journal.CQC > CurrentRank.CQC)
                promotions.Add($"CQC rank {journal.CQC}");
            if (journal.Soldier > CurrentRank.Soldier)
                promotions.Add($"Soldier rank {journal.Soldier}");

            CurrentRank = journal;
            if (promotions.Count == 0)
                return;

            var log = new BridgeLog(this, journal);
            log.TitleSsml.Append("Promotion");

            log.DetailSsml.Append("Congratulations, Commander. You have been promoted to ");
            if (promotions.Count <= 2)
                log.DetailSsml.Append(String.Join(" and ", promotions) + ".");
            else
                log.DetailSsml.Append(String.Join(", ", promotions.Take(promotions.Count - 1)) + " and " + promotions.Last() + ".");

            LogEvent(log);
        }

        public void DoFuelScoop(FuelScoop journal)
        {
            var log = new BridgeLog(this, journal);
            log.TitleSsml.Append("Fuel Scooping");
            log.DetailSsml
                .Append($"Scooped")
                .AppendNumber(Math.Round(journal.Scooped, 2))
                .Append("tons, tank at")
                .AppendNumber(Math.Round(journal.Total, 2))
                .Append("tons.");

            float total = (float)Math.Round(journal.Total, 2);
            if (total == 128 || total == 64 || total == 32 || total == 16 || total == 8 || total == 4)
                log.DetailSsml.Append("Tank full");

            LogEvent(log);
        }

        public void DoApproachBody(ApproachBody journal)
        {
            var log = new BridgeLog(this, journal);
            log.TitleSsml.Append("Flight Operations");

            log.DetailSsml.Append($"On approach to body")
                .AppendBodyName(journal.Body);

            LogEvent(log);
        }

        public void DoLeaveBody(LeaveBody journal)
        {
            var log = new BridgeLog(this,journal);
            log.TitleSsml.Append("Flight Operations");

            log.DetailSsml.Append($"Departing body")
                .AppendBodyName(GetBodyName(journal.Body));

            LogEvent(log);
        }

        public void DoTouchdown(Touchdown journal)
        {
            var log = new BridgeLog(this, journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml
                .Append($"Touchdown on body")
                .AppendBodyName(GetBodyName(journal.Body))
                .Append("completed")
                .AppendEmphasis("Commander", EmphasisType.Moderate);

            LogEvent(log);
        }

        public void DoLiftoff(Liftoff journal)
        {
            var log = new BridgeLog(this,journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml
                .Append($"Liftoff complete from body")
                .AppendBodyName(GetBodyName(journal.Body));

            LogEvent(log);
        }

        public void DoDockingGranted(DockingGranted journal)
        {
            var log = new BridgeLog(this, journal);
            log.TitleSsml.Append("Flight Operations");

            log.DetailSsml
                .Append($"{journal.StationName} Tower has granted our docking request, Commander. Heading to landing pad")
                .AppendEmphasis(journal.LandingPad.ToString(), EmphasisType.Moderate).EndSentence();

            LogEvent(log);
        }

        public void DoDockingDenied(DockingDenied journal)
        {
            var log = new BridgeLog(this, journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.Append($"{journal.StationName} Tower has denied our docking request.");

            switch (journal.Reason)
            {
                case Observatory.Framework.Files.ParameterTypes.Reason.TooLarge:
                    log.DetailSsml.Append("Our ship is too large for their landing pads, Commander.");
                    break;
                case Observatory.Framework.Files.ParameterTypes.Reason.Offences:
                    log.DetailSsml.Append("Apparently we have outstanding offences against them, Commander. We might want to rectify that first.");
                    break;
                case Observatory.Framework.Files.ParameterTypes.Reason.DockOffline:
                    log.DetailSsml.Append("Their docking system is offline, Commander. We may have to do this one manually.");
                    break;
                case Observatory.Framework.Files.ParameterTypes.Reason.ActiveFighter:
                    log.DetailSsml.Append("We have an active fighter in flight, Commander. We better bring them back on board first.");
                    break;
                case Observatory.Framework.Files.ParameterTypes.Reason.Distance:
                    log.DetailSsml.Append("We made the request a bit early, Commander. Let's close to within 7.5 kilometers and try to resubmit.");
                    break;
                case Observatory.Framework.Files.ParameterTypes.Reason.NoSpace:
                    log.DetailSsml.Append("Sorry, Commander. No room at the inn. All landing pads are occupied.");
                    break;
                case Observatory.Framework.Files.ParameterTypes.Reason.RestrictedAccess:
                    log.DetailSsml.Append("Looks like access is restricted, Commander.");
                    break;
                default:
                    log.DetailSsml.Append("No specific reason given. Guess they don't like us, Commander.");
                    break;
            }

            LogEvent(log);
        }

        public void DoDockingCancelled(DockingCancelled journal)
        {
            var log = new BridgeLog(this, journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.Append($"{journal.StationName} Tower has cancelled our docking request, Commander. We'll need to resubmit another request if we want to dock.");

            LogEvent(log);
        }

        public void DoDocked(Docked journal)
        {
            var log = new BridgeLog(this, journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.Append($"{journal.StationName} Tower, we have completed docking.");

            LogEvent(log);
        }

        public void DoUndocked(Undocked journal)
        {
            var log = new BridgeLog(this, journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.Append($"{journal.StationName} Tower, we have cleared the pad and are on the way out.");

            LogEvent(log);
        }

        public void DoSupercruiseExit(SupercruiseExit journal)
        {
            var log = new BridgeLog(this,journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.Append($"Exiting super-cruise, sub-light engines active.");

            LogEvent(log);
        }

        public void DoSupercruiseEntry(SupercruiseEntry journal)
        {
            var log = new BridgeLog(this,journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.Append($"Super-cruising, FSD active.");

            LogEvent(log);
        }

        public void DoFSDJump(FSDJump journal)
        {
            var log = new BridgeLog(this,journal);
            log.TitleSsml.Append("Flight Operations");

            log.DetailSsml
                .Append($"Jump completed, commander. Arrived at")
                .AppendBodyName(journal.StarSystem)
                .Append("We travelled")
                .AppendNumber(Math.Round(journal.JumpDist, 2))
                .Append("light years, using")
                .AppendNumber(Math.Round(journal.FuelUsed, 2))
                .Append("tons of fuel.");

            _core.ExecuteOnUIThread(() => {
                // Remove all entries except for the last Start Jump entry
                var keep = _events.Cast<BridgeLog>().LastOrDefault(e => e.Title.StartsWith("FSD Jump"));
                while (_events.Count > 0)
                {
                    if (_events[0] == keep)
                        break;
                    _events.RemoveAt(0);
                }
            });

            LogEvent(log);

            CurrentSystem = new CurrentSystemData(journal);
        }

        public void DoStartJump(StartJump journal)
        {
            // We get this event when entering supercruise if we have a destination locked
            if (!String.IsNullOrEmpty(journal.StarSystem))
            {
                var log = new BridgeLog(this,journal);
                log.TitleSsml.Append("Flight Operations");

                var scoopable = _scoopableStars.Contains(journal.StarClass) ? ", scoopable" : ", non-scoopable";
                log.DetailSsml.Append($"Destination star is type {journal.StarClass}{scoopable}.");

                if (journal.StarClass.IsNeutronStar() || journal.StarClass.IsWhiteDwarf())
                {
                    log.DetailSsml.AppendEmphasis("Commander,", EmphasisType.Moderate);
                    log.DetailSsml.Append("this is a hazardous star type.");
                    log.DetailSsml.AppendEmphasis("Throttle down now.", EmphasisType.Strong);
                }

                LogEvent(log);
            }
        }

        public void DoFSSAllBodiesFound(FSSAllBodiesFound journal)
        {
            if (CurrentSystem.ScanComplete)
                return;

            var log = new BridgeLog(this, journal);
            log.TitleSsml.Append("Science Station");
            log.DetailSsml.Append($"System Scan Complete, commander. We've found all bodies in this system.");

            LogEvent(log);
            CurrentSystem.ScanComplete = true;
        }

        public void DoScan(Scan journal)
        {
            // Can be sent again after a DSS, so we simply add in a bit more detail
            if (CurrentSystem.ScannedBodies.ContainsKey(journal.BodyName))
                return;

            CurrentSystem.ScannedBodies.Add(journal.BodyName, journal);
            CurrentSystem.BodySignals.TryGetValue(journal.BodyName, out var signals);


            if (!String.IsNullOrEmpty(journal.StarType))
            {
                var log = new BridgeLog(this,journal);
                log.IsTitleSpoken = true;

                if (Int32.TryParse(GetBodyName(journal.BodyName), out int bodyNumber))
                    log.TitleSsml.Append("Body").AppendBodyName(GetBodyName(journal.BodyName));
                else
                    log.TitleSsml.Append("Star").AppendBodyName(GetBodyName(journal.BodyName));

                var scoopable = _scoopableStars.Contains(journal.StarType.Substring(0, 1)) ? ", scoopable" : ", non-scoopable";

                if (journal.StarType.IsBlackHole())
                    log.DetailSsml.AppendUnspoken(Emojis.BlackHole);
                else if (journal.StarType.IsWhiteDwarf())
                    log.DetailSsml.AppendUnspoken(Emojis.WhiteDwarf);
                else
                    log.DetailSsml.AppendUnspoken(Emojis.Solar);

                log.DetailSsml.Append($"{BridgeUtils.GetStarTypeName(journal.StarType)}{scoopable}.");

                var estimatedValue = BodyValueEstimator.GetStarValue(journal.StarType, !journal.WasDiscovered);
                if (estimatedValue >= Options.HighValueBody)
                {
                    log.DetailSsml.AppendUnspoken(Emojis.HighValue);
                    log.DetailSsml.Append($"Estimated value");
                    log.DetailSsml.AppendNumber(estimatedValue);
                    log.DetailSsml.Append("credits.");
                }
                else
                {
                    log.DetailSsml.AppendUnspoken($"Estimated value {estimatedValue:n0} credits.");
                }

                LogEvent(log);
            }
            else if (!String.IsNullOrEmpty(journal.PlanetClass)) // ignore belt clusters
            {
                var log = new BridgeLog(this,journal);
                log.IsTitleSpoken = true;
                log.TitleSsml.Append("Body").AppendBodyName(GetBodyName(journal.BodyName));

                if (journal.PlanetClass.IsEarthlike())
                    log.DetailSsml.AppendUnspoken(Emojis.Earthlike);
                else if (journal.PlanetClass.IsWaterWorld())
                    log.DetailSsml.AppendUnspoken(Emojis.WaterWorld);
                else if (journal.PlanetClass.IsHighMetalContent())
                    log.DetailSsml.AppendUnspoken(Emojis.HighMetalContent);
                else if (journal.PlanetClass.IsAmmoniaWorld())
                    log.DetailSsml.AppendUnspoken(Emojis.Ammonia);
                else
                    log.DetailSsml.AppendUnspoken(Emojis.OtherBody);

                if (!String.IsNullOrEmpty(journal.TerraformState))
                    log.DetailSsml.AppendUnspoken(Emojis.Terraformable);

                if (!String.IsNullOrEmpty(journal.TerraformState))
                    log.DetailSsml.Append($"{journal.TerraformState}");

                log.DetailSsml.AppendBodyType(journal.PlanetClass);
                if (journal.Landable)
                {
                    if (!String.IsNullOrEmpty(journal.Atmosphere))
                        log.DetailSsml.Append($", landable with {journal.Atmosphere}.");
                    else
                        log.DetailSsml.Append(", landable no atmosphere.");
                }
                else
                    log.DetailSsml.EndSentence();

                var k_value = BodyValueEstimator.GetKValueForBody(journal.PlanetClass, !String.IsNullOrEmpty(journal.TerraformState));
                var estimatedValue = BodyValueEstimator.GetBodyValue(k_value, journal.MassEM, !journal.WasDiscovered, true, !journal.WasMapped, true);
                if (estimatedValue >= Options.HighValueBody)
                {
                    log.DetailSsml.AppendUnspoken(Emojis.HighValue);
                    log.DetailSsml.Append($"Estimated value");
                    log.DetailSsml.AppendNumber(estimatedValue);
                    log.DetailSsml.Append("credits.");
                }
                else
                {
                    log.DetailSsml.AppendUnspoken($"Estimated value {estimatedValue:n0} credits.");
                }

                if (signals != null)
                {
                    List<string> list = new List<string>();
                    bool hasBio = false;
                    bool hasGeo = false;
                    foreach (var signal in signals.Signals)
                    {
                        list.Add($"{signal.Count} {signal.Type_Localised}");
                        if (signal.Type_Localised.StartsWith("Geo", StringComparison.OrdinalIgnoreCase))
                            hasGeo = true;
                        if (signal.Type_Localised.StartsWith("Bio", StringComparison.OrdinalIgnoreCase))
                            hasBio = true;
                    }

                    int total = signals.Signals.Sum(s => s.Count);
                    string signalsText = total == 1 ? "signal" : "signals";

                    if (hasBio)
                        log.DetailSsml.AppendUnspoken(Emojis.BioSignals);
                    if (hasGeo)
                        log.DetailSsml.AppendUnspoken(Emojis.GeoSignals);

                    if (list.Count <= 2)
                        log.DetailSsml.Append($"Sensors found {String.Join(" and ", list)} {signalsText}.");
                    else
                        log.DetailSsml.Append($"Sensors found {String.Join(", ", list.Take(list.Count - 1))} and {list.Last()} {signalsText}.");
                }

                LogEvent(log);
            }
        }

        public void DoFSSDiscoveryScan(FSSDiscoveryScan journal)
        {
            var log = new BridgeLog(this,journal);
            log.TitleSsml.Append("Science Station");

            log.DetailSsml.Append($"Discovery scan found {journal.BodyCount} bodies, Commander.");
            log.DetailSsml.Append($"Progress is {journal.Progress * 100:n0} percent.");

            LogEvent(log);
        }

        public void DoFSSSignalDiscovered(FSSSignalDiscovered journal)
        {
            if (!String.IsNullOrEmpty(journal.USSType_Localised))
            {
                var log = new BridgeLog(this,journal);
                log.TitleSsml.Append("Science Station");

                log.DetailSsml.Append($"Sensors are picking up {journal.USSType_Localised} signal, Commander. Threat level {journal.ThreatLevel}.");
                var minutes = (int)Math.Truncate(journal.TimeRemaining) / 60;
                var seconds = (int)Math.Truncate(journal.TimeRemaining) % 60;
                if (minutes > 0 || seconds > 0)
                {
                    log.DetailSsml.Append($"{minutes} " + (minutes == 1 ? "minute" : "minutes"));
                    log.DetailSsml.Append($"and {seconds} " + (seconds == 1 ? "second" : "seconds") + " remaining.");
                }
                LogEvent(log);
            }
        }

        public void DoFSSBodySignals(FSSBodySignals journal)
        {
            CurrentSystem.BodySignals[journal.BodyName] = journal;
        }

        public void DoSAAScanComplete(SAAScanComplete journal)
        {
            var log = new BridgeLog(this,journal);
            log.IsTitleSpoken = true;
            log.TitleSsml.Append("Body").AppendBodyName(GetBodyName(journal.BodyName));

            if (journal.ProbesUsed < journal.EfficiencyTarget)
                log.DetailSsml.Append($"Surface Scan complete, with efficiency bonus, using only {journal.ProbesUsed} probes, commander.");
            else
                log.DetailSsml.Append($"Surface Scan complete using {journal.ProbesUsed} probes, commander.");

            if (journal.Mappers == null || journal.Mappers.Count == 0)
            {
                log.DetailSsml.AppendEmphasis("Commander,", EmphasisType.Moderate);
                log.DetailSsml.Append($"we are the first to map body");
                log.DetailSsml.AppendBodyName(GetBodyName(journal.BodyName));

            }
            else
            {
                log.DetailSsml.Append("Body").AppendBodyName(GetBodyName(journal.BodyName)).Append("is");
            }

            if (CurrentSystem.ScannedBodies.TryGetValue(journal.BodyName, out Scan scan))
            {
                string article = "a ";
                string terraformable = "";
                if (!String.IsNullOrEmpty(scan.TerraformState))
                    terraformable = "terraformable ";
                else if (scan.PlanetClass.IndexOfAny("aeiou".ToCharArray()) == 0)
                    article = "an ";

                log.DetailSsml.Append($", {article}{terraformable}");
                log.DetailSsml.AppendBodyType(scan.PlanetClass);
            }

            LogEvent(log);
        }

        public void DoSAASignalsFound(SAASignalsFound journal)
        {
            var log = new BridgeLog(this,journal);
            log.TitleSsml.Append("Body").AppendBodyName(GetBodyName(journal.BodyName));

            List<string> signals = new List<string>();
            bool hasGeo = false;
            bool hasBio = false;
            foreach (var signal in journal.Signals)
            {
                signals.Add($"{signal.Count} {signal.Type_Localised}");
                if (signal.Type_Localised.StartsWith("Geo", StringComparison.OrdinalIgnoreCase))
                    hasGeo = true;
                if (signal.Type_Localised.StartsWith("Bio", StringComparison.OrdinalIgnoreCase))
                    hasBio = true;
            }

            var total = journal.Signals.Sum(s => s.Count);
            var plural = total == 1 ? "signal" : "signals";

            if (hasBio)
                log.DetailSsml.AppendUnspoken(Emojis.BioSignals);
            if (hasGeo)
                log.DetailSsml.AppendUnspoken(Emojis.GeoSignals);

            if (signals.Count <= 2)
                log.DetailSsml
                    .Append($"Sensors are picking up {String.Join(" and ", signals)} {plural} on body")
                    .AppendBodyName(GetBodyName(journal.BodyName));
            else
                log.DetailSsml
                    .Append($"Sensors are picking up {String.Join(", ", signals.Take(signals.Count - 1))} and {signals.Last()} {plural} on body")
                    .AppendBodyName(GetBodyName(journal.BodyName));

            LogEvent(log);
        }

        public void DoLaunchSRV(LaunchSRV journal)
        {
            var log = new BridgeLog(this,journal);
            log.TitleSsml.Append("Away Team");
            log.DetailSsml.Append($"Deploying {journal.SRVType_Localised} with {journal.Loadout} load-out, commander.");

            LogEvent(log);
        }

        public void DoDockSRV(DockSRV journal)
        {
            var log = new BridgeLog(this,journal);
            log.TitleSsml.Append("Away Team");
            log.DetailSsml.Append($"{journal.SRVType_Localised} returned to SRV docking bay, commander.");

            LogEvent(log);
        }

    }
}