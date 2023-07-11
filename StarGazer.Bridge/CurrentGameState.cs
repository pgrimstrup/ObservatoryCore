using System.Xml.Linq;
using Observatory.Framework.Files;
using Observatory.Framework.Files.Journal;
using Observatory.Framework.Files.ParameterTypes;
using StarGazer.Bridge.Events;

namespace StarGazer.Bridge
{
    internal class CurrentGameState
    {
        public ulong ShipId { get; set; }
        public string? ShipType { get; set; } 
        public string? ShipName { get; set; } 
        public string? Commander { get; set; } 
        public long Credits { get; set; }
        public double FuelCapacity { get; set; }
        public StatusFlags Status { get; set; }
        public StatusFlags2 Status2 { get; set; }
        public FuelType Fuel { get; private set; } = new FuelType();
        public double FuelScooped { get; set; }

        // Tracked after jumping into a system
        public JumpDestination CurrentSystem { get; } = new JumpDestination();
        public InSystemDestination CurrentLocation { get; } = new InSystemDestination();
        public InSystemDestination TargetLocation { get; } = new InSystemDestination();

        // Tracked after jumping into a system or selecting a new destination
        public JumpDestination JumpDestination { get; } = new JumpDestination();

        // Tracked during NavRoute handler
        public JumpDestination RouteDestination { get; } = new JumpDestination();
        public InSystemDestination RouteDestinationLocation { get; } = new InSystemDestination();

        public DateTime DestinationTimeToSpeak { get; set; }
        public DateTime RemainingJumpsInRouteTimeToSpeak { get; set; }

        public int ScanPercent { get; set; }

        // List of Detailed/Auto scans of each body in the system as they occur
        public Dictionary<string, Scan> ScannedBodies { get; } = new Dictionary<string, Scan>();
        public int AutoCompleteScanCount { get; set; }

        // List of Signals detected on each body in the system. We get these before the Detailed scan completes,
        // so we track them until the Scan has finished
        public Dictionary<string, FSSBodySignals> BodySignals { get; } = new Dictionary<string, FSSBodySignals>();

        // List of stations detected in the system
        public List<string> Stations { get; } = new List<string>();

        // List of carriers detected in the system. Keyed by registration
        public Dictionary<string, string> Carriers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public void Assign(JournalBase journal)
        {
            switch (journal)
            {
                case SetUserShipName setUserShipName:
                    ShipId = setUserShipName.ShipID;
                    ShipName = setUserShipName.UserShipName;
                    break;
                case ShipyardBuy shipyardBuy:
                    ShipId = 0;
                    ShipName = null;
                    break;
                case ShipyardSwap shipyardSwap:
                    ShipId = shipyardSwap.ShipID;
                    ShipName = null;
                    break;
                case Loadout loadout:
                    ShipId = loadout.ShipID;
                    ShipName = loadout.ShipName;
                    FuelCapacity = loadout.FuelCapacity.Main; // assume full tank until first status update
                    Fuel = new FuelType {
                        FuelMain = loadout.FuelCapacity.Main,
                        FuelReservoir = loadout.FuelCapacity.Reserve
                    };
                    break;
                case Undocked undocked:
                    CurrentLocation.Set(CurrentLocation.SystemAddress, "");
                    break;
                case Location location:
                    AssignLocation(location);
                    break;
                case LoadGame loadGame:
                    AssignLoadGame(loadGame);
                    break;
                case FuelScoop fuelScoop:
                    AssignFuelScoop(fuelScoop);
                    break;
                case Status status:
                    AssignStatus(status);
                    break;
                case FSDTarget fsdTarget:
                    AssignFSDTarget(fsdTarget);
                    break;
                case CarrierJump carrierJump:
                    AssignCarrierJump(carrierJump);
                    break;
                case FSDJump fsdJump:
                    AssignFSDJump(fsdJump);
                    break;
                case Docked docked:
                    // Not changing star system
                    CurrentLocation.Set(docked.SystemAddress, docked.StationName);
                    break;
            }
        }

        public void AssignLocation(Location location)
        {
            Bridge.Instance.ResetLogEntries();
            CurrentSystem.Set(location.SystemAddress, location.StarSystem, "", location.StarPos);
            var match = BaseEventHandler.CarrierNameRegex.Match(location.StationName);
            if (match.Success && !String.IsNullOrWhiteSpace(match.Groups[1].Value))
                Carriers[match.Groups[2].Value] = match.Groups[1].Value.Trim();

            if (location.Docked)
                CurrentLocation.Set(location.SystemAddress, location.StationName);
            else
                CurrentLocation.Set(location.SystemAddress, location.BodyID, location.Body);
        }

        public void AssignFuelScoop(FuelScoop journal)
        {
            FuelScooped += journal.Scooped;
            Fuel = new FuelType {
                FuelMain = journal.Total,
                FuelReservoir = Fuel.FuelReservoir
            };
        }


        public void AssignStatus(Status status)
        {
            if (status.Fuel != null)
                Fuel = new FuelType {
                    FuelMain = status.Fuel.FuelMain,
                    FuelReservoir = status.Fuel.FuelReservoir
                };
        }

        public void AssignLoadGame(LoadGame load)
        {
            ShipType = String.IsNullOrWhiteSpace(load.Ship_Localised) ? load.Ship : load.Ship_Localised;
            ShipName = load.ShipName;
            Commander = load.Commander;
            Credits = load.Credits;
            FuelCapacity = load.FuelCapacity;
            Status = StatusFlags.MainShip;
            Fuel = new FuelType {
                FuelMain = load.FuelLevel,
                FuelReservoir = 0
            };

            if (load.StartLanded)
                Status |= StatusFlags.Landed | StatusFlags.LandingGear | StatusFlags.Masslock | StatusFlags.LatLongValid | StatusFlags.MainShip;
            else if (!load.StartDead)
                Status |= StatusFlags.MainShip;
        }

        // Occurs mid-jump, before the FSDJump event
        public void AssignFSDTarget(FSDTarget target)
        {
            TargetLocation.Clear();
            if (Status.HasFlag(StatusFlags.FSDJump))
            {
                // Let's assume we will arrive at our intended destination
                CurrentSystem.Set(JumpDestination.SystemAddress, JumpDestination.StarSystem, JumpDestination.StarClass, JumpDestination.StarPos);
            }
            else
            {
                DestinationTimeToSpeak = DateTime.Now;
            }

            // Setting new destination for the next jump. 
            JumpDestination.Set(target.SystemAddress, target.Name, target.StarClass, (0, 0, 0));
            if (target.RemainingJumpsInRoute != JumpDestination.RemainingJumpsInRoute)
                RemainingJumpsInRouteTimeToSpeak = DateTime.Now;

            JumpDestination.RemainingJumpsInRoute = target.RemainingJumpsInRoute;
        }

        // Jump completed - we have entered a new star system
        public void AssignFSDJump(FSDJump jump)
        {
            ScanPercent = 0;
            AutoCompleteScanCount = 0;
            ScannedBodies.Clear();
            BodySignals.Clear();
            Stations.Clear();
            Carriers.Clear();

            CurrentLocation.Clear();
            TargetLocation.Clear();

            CurrentSystem.Set(jump.SystemAddress, jump.StarSystem, CurrentSystem.StarClass, jump.StarPos);
        }

        // Fleet Carrier Jump - same as FSDJump, except we don't clear current location as we are on the Fleet Carrier
        public void AssignCarrierJump(CarrierJump jump)
        {
            ScanPercent = 0;
            AutoCompleteScanCount = 0;
            ScannedBodies.Clear();
            BodySignals.Clear();
            Stations.Clear();
            Carriers.Clear();

            TargetLocation.Clear();

            CurrentSystem.Set(jump.SystemAddress, jump.StarSystem, CurrentSystem.StarClass, jump.StarPos);
        }

    }
}
