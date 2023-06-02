using Observatory.Framework.Files;
using Observatory.Framework.Files.ParameterTypes;
using StarGazer.Framework;

namespace StarGazer.Bridge.Events
{
    internal class StatusChangedEventHandler : BaseEventHandler, IJournalEventHandler<Status>
    {
        public enum FuelWarnings
        {
            Plenty,
            Below50,
            Below25,
            Below10,
            Below5,
            Below1
        }

        public List<(double, FuelWarnings, string)> FuelWarning = new List<(double, FuelWarnings, string)> {
            { new (0.01, FuelWarnings.Below1, "fuel level is critical. Main Fuel tank is below 1 percent.") },
            { new (0.05, FuelWarnings.Below5, "fuel level is dangerously low. Main Fuel tank is below 5 percent.") },
            { new (0.10, FuelWarnings.Below10, "fuel level is very low. Main Fuel tank is below 10 percent.") },
            { new (0.25, FuelWarnings.Below25, "fuel level is low. Main Fuel tank is below 25 percent.") },
            { new (0.50, FuelWarnings.Below50, "fuel level is down to 50 percent.") }
        };

        FuelWarnings _shipFuelWarnings;
        FuelWarnings _srvFuelWarnings;

        private bool HasStatusChanged(Status newStatus)
        {
            return GameState.Status != newStatus.Flags || GameState.Status2 != newStatus.Flags2;
        }

        private bool HasShipStatusChanged(StatusFlags flag, Status newStatus)
        {
            if (GameState.Status.HasFlag(StatusFlags.MainShip) && newStatus.Flags.HasFlag(StatusFlags.MainShip))
                return (GameState.Status & flag) != (newStatus.Flags & flag);
            return false;
        }

        private bool HasSrvStatusChanged(StatusFlags flag, Status newStatus)
        {
            if (GameState.Status.HasFlag(StatusFlags.SRV) && newStatus.Flags.HasFlag(StatusFlags.SRV))
                return (GameState.Status & flag) != (newStatus.Flags & flag);
            return false;
        }

        private bool HasOnFootStatusChanged(StatusFlags2 flag, Status newStatus)
        {
            if (GameState.Status2.HasFlag(StatusFlags2.OnFoot) && newStatus.Flags2.HasFlag(StatusFlags2.OnFoot))
                return (GameState.Status2 & flag) != (newStatus.Flags2 & flag);
            return false;
        }

        private bool IsHyperdriveCharging(StatusFlags flags, StatusFlags2 flags2)
        {
            return flags.HasFlag(StatusFlags.FSDCharging) && flags2.HasFlag(StatusFlags2.FsdHyperdriveCharging);
        }

        private bool IsSupercruiseCharging(StatusFlags flags, StatusFlags2 flags2)
        {
            return flags.HasFlag(StatusFlags.FSDCharging) && !flags2.HasFlag(StatusFlags2.FsdHyperdriveCharging);
        }



        public void HandleEvent(Status newStatus)
        {
            if (newStatus.Flags.HasFlag(StatusFlags.MainShip))
            {
                CheckShipFuelLevel(newStatus);
                CheckDestination(newStatus);
                CheckJumpDestination(newStatus);
            }

            if (newStatus.Flags.HasFlag(StatusFlags.SRV))
                CheckSrvFuelLevel(newStatus);

            if (GameState.Status == newStatus.Flags && GameState.Status2 == newStatus.Flags2)
                return;

            if (HasStatusChanged(newStatus))
            {
                LogInfo($"StatusChanged: Flags : {GameState.Status} -> {newStatus.Flags}");
                LogInfo($"             : Flags2: {GameState.Status2} -> {newStatus.Flags2}");
            }

            // Seems to be a bug, we get the masslock flag sometimes when we shouldn't
            if (!newStatus.Flags.HasFlag(StatusFlags.FSDJump) &&
                !newStatus.Flags.HasFlag(StatusFlags.Supercruise) &&
                !newStatus.Flags.HasFlag(StatusFlags.Landed))
            {
                if (HasShipStatusChanged(StatusFlags.Masslock, newStatus))
                    DoMasslock(newStatus);

                // Sometimes get a change to LandingGear when already landed
                if (HasShipStatusChanged(StatusFlags.LandingGear, newStatus))
                    DoLandingGear(newStatus);
            }

            // Update current status
            GameState.Status = newStatus.Flags;
            GameState.Status2 = newStatus.Flags2;
        }

        private void CheckDestination(Status status)
        {
            // Only interested in destinations that are bodies within a system
            if (status.Destination == null)
                return;

            if (status.Destination.Body > 0 && GameState.Status != 0)
            {
                if (GameState.DestinationName != status.Destination.Name)
                {
                    GameState.DestinationName = status.Destination.Name;
                    GameState.DestinationStarClass = "";

                    var log = new BridgeLog(status);
                    log.TitleSsml.Append("Flight Operations");
                    log.DetailSsml
                        .Append("Course laid in to")
                        .AppendBodyName(GetBodyName(GameState.DestinationName));

                    log.Send();
                    GameState.DestinationTimeToSpeak = DateTime.Now.Add(SpokenDestinationInterval);
                }
            }
        }

        private void CheckJumpDestination(Status status)
        {
            if (IsSupercruiseCharging(status.Flags, status.Flags2) && !IsSupercruiseCharging(GameState.Status, GameState.Status2))
            {
                BridgeLog log = new BridgeLog(status);
                log.SpokenOnly();
                log.DetailSsml.Append("Preparing for supercruise.");
                log.Send();
            }

            // Only process if we have changed state to charging FSD for Hyperdrive Jump
            if (IsHyperdriveCharging(status.Flags, status.Flags2) && !IsHyperdriveCharging(GameState.Status, GameState.Status2))
            {
                BridgeLog log = new BridgeLog(status);
                log.SpokenOnly();

                if (GameState.DestinationTimeToSpeak > DateTime.Now || String.IsNullOrWhiteSpace(GameState.DestinationName))
                {
                    log.DetailSsml.Append("Preparing to jump.");
                }
                else
                {
                    var fuelStar = GameState.DestinationStarClass.IsFuelStar() ? ", a fuel star" : "";
                    log.DetailSsml
                        .Append("Preparing to jump to")
                        .AppendBodyName(GameState.DestinationName)
                        .EndSentence()
                        .Append($"Destination star is a")
                        .AppendBodyType(GetStarTypeName(GameState.DestinationStarClass))
                        .Append($"{fuelStar}.");
                }

                if (GameState.RemainingJumpsInRoute == 1)
                    log.DetailSsml.Append($"This is the final jump in the current flight plan.");
                else if (GameState.RemainingJumpsInRoute > 1 && GameState.RemainingJumpsInRouteTimeToSpeak < DateTime.Now && (GameState.RemainingJumpsInRoute < 5 || (GameState.RemainingJumpsInRoute % 5) == 0))
                {
                    log.DetailSsml.Append($"There are {GameState.RemainingJumpsInRoute} jumps remaining in the current flight plan.");
                    GameState.RemainingJumpsInRouteTimeToSpeak = DateTime.Now.Add(SpokenDestinationInterval * 2);
                }

                if (GameState.DestinationStarClass.IsNeutronStar() || GameState.DestinationStarClass.IsWhiteDwarf() || GameState.DestinationStarClass.IsBlackHole())
                {
                    log.DetailSsml
                        .Append("That is a hazardous star type")
                        .AppendEmphasis("Commander.", EmphasisType.Moderate)
                        .Append("We should throttle down before exiting jump.");
                }

                log.Send();
                if (!Bridge.Instance.Core.IsLogMonitorBatchReading)
                {
                    if (String.IsNullOrWhiteSpace(GameState.DestinationName) && !String.IsNullOrWhiteSpace(status.Destination.Name))
                        GameState.DestinationTimeToSpeak = DateTime.Now;
                    else
                        GameState.DestinationTimeToSpeak = DateTime.Now.Add(SpokenDestinationInterval);
                }
            }
        }

        private void CheckSrvFuelLevel(Status status)
        {
            BridgeLog? log = null;
            double fuelLevel = status.Fuel.FuelReservoir / 0.5;
            FuelWarnings newWarning = FuelWarnings.Plenty;
            foreach ((double level, FuelWarnings warning, string message) item in FuelWarning)
            {
                if (fuelLevel <= item.level)
                {
                    if (_srvFuelWarnings < item.warning)
                    {
                        log = new BridgeLog(status);
                        log.SpokenOnly()
                            .DetailSsml.AppendEmphasis("Commander,", EmphasisType.Moderate)
                            .Append("SRV " + item.message);
                    }
                    newWarning = item.warning;
                    break;
                }
            }
            _srvFuelWarnings = newWarning;

            if (log != null)
                Bridge.Instance.LogEvent(log);

        }

        private void CheckShipFuelLevel(Status status)
        {
            GameState.Assign(status);
            if (GameState.FuelCapacity == 0)
                return;

            BridgeLog? log = null;
            if (GameState.Status.HasFlag(StatusFlags.FuelScooping) && !status.Flags.HasFlag(StatusFlags.FuelScooping) && GameState.FuelScooped > 0)
            {
                log = new BridgeLog(status);
                log.TitleSsml.Append("Fuel Scooping");

                log.DetailSsml.AppendUnspoken(Emojis.FuelScoop);
                log.DetailSsml
                    .Append($"Fuel scooping terminated after collecting")
                    .AppendNumber(Math.Round(GameState.FuelScooped, 2))
                    .Append("tons.");

                double total = Math.Round(GameState.Fuel.FuelMain, 2);
                log.DetailSsml.Append("Main tank at")
                    .AppendNumber(total)
                    .Append("tons.");


                Bridge.Instance.LogEvent(log);
                log = null;

                GameState.FuelScooped = 0;
            }

            double fuelLevel = GameState.Fuel.FuelMain / GameState.FuelCapacity;
            FuelWarnings newWarning = FuelWarnings.Plenty;
            foreach ((double level, FuelWarnings warning, string message) item in FuelWarning)
            {
                if (fuelLevel <= item.level)
                {
                    if (_shipFuelWarnings < item.warning)
                    {
                        log = new BridgeLog(status);
                        log.SpokenOnly()
                            .DetailSsml.AppendEmphasis("Commander,", EmphasisType.Moderate)
                            .Append("Ship's main " + item.message);
                    }
                    newWarning = item.warning;
                    break;
                }
            }
            _shipFuelWarnings = newWarning;

            if (log != null)
                Bridge.Instance.LogEvent(log);
        }

        private void DoMasslock(Status newstatus)
        {
            var log = new BridgeLog(newstatus);
            log.SpokenOnly();

            if (newstatus.Flags.HasFlag(StatusFlags.Masslock))
            {
                log.TitleSsml.Append("Flight Operations");
                log.DetailSsml.Append("Mass lock, FSD unavailable");
            }
            else
            {
                log.TitleSsml.Append("Flight Operations");
                log.DetailSsml.Append("Left mass lock, FSD available");
            }
            Bridge.Instance.LogEvent(log);
        }

        private void DoLandingGear(Status newstatus)
        {
            var log = new BridgeLog(newstatus);
            log.SpokenOnly();

            if (newstatus.Flags.HasFlag(StatusFlags.LandingGear))
            {
                log.TitleSsml.Append("Flight Operations");
                log.DetailSsml.Append("Deploying landing gear");
            }
            else
            {
                log.TitleSsml.Append("Flight Operations");
                log.DetailSsml.Append("Landing gear up");
            }
            Bridge.Instance.LogEvent(log);
        }

    }
}
