using Observatory.Framework.Files;
using Observatory.Framework.Files.Journal;
using Observatory.Framework.Files.ParameterTypes;

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

        // Transient properties for use by EDSM
        public string? SystemName { get; set; } 
        public string? StationName { get; set; }

        // Tracked during FSDTarget for later use
        public string? NextSystemName { get; set; } 
        public string? NextStarClass { get; set; } 
        public int RemainingJumpsInRoute { get; set; }

        public DateTime FirstDiscoverySpoken { get; set; }
        public DateTime RemainingJumpsInRouteToSpeak { get; set; }
        public DateTime NextDestinationTimeToSpeak { get; set; }

        public int ScanPercent { get; set; }

        // List of Detailed/Auto scans of each body in the system as they occur
        public Dictionary<string, Scan> ScannedBodies { get; } = new Dictionary<string, Scan>();

        // List of Signals detected on each body in the system. We get these before the Detailed scan completes,
        // so we track them until the Scan has finished
        public Dictionary<string, FSSBodySignals> BodySignals { get; } = new Dictionary<string, FSSBodySignals>();

        public void Assign(JournalBase journal)
        {
            switch (journal)
            {
                case LoadGame loadGame: 
                    AssignLoadGame(loadGame); 
                    break;
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
                    break;
                case Undocked undocked:
                    StationName = null;
                    break;
                case Location location:
                    SystemName = location.StarSystem;
                    StationName = location.StationName;
                    break;
                case FSDTarget fsdTarget:
                    AssignFSDTarget(fsdTarget);
                    break;
                case FSDJump fsdJump:
                    AssignFSDJump(fsdJump);
                    break;
                case Docked docked:
                    SystemName = docked.StarSystem;
                    StationName = docked.StationName;
                    break;
                case Status status:
                    AssignStatus(status);
                    break;
                case FuelScoop fuelScoop:
                    AssignFuelScoop(fuelScoop);
                    break;
            }
        }

        private void AssignFuelScoop(FuelScoop journal)
        {
            FuelScooped += journal.Scooped;
            Fuel = new FuelType {
                FuelMain = journal.Total,
                FuelReservoir = Fuel.FuelReservoir
            };
        }


        void AssignStatus(Status status)
        {
            if (status.Fuel != null)
                Fuel = new FuelType {
                    FuelMain = status.Fuel.FuelMain,
                    FuelReservoir = status.Fuel.FuelReservoir
                };
        }

        void AssignLoadGame(LoadGame load)
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

        void AssignFSDTarget(FSDTarget target)
        {
            RemainingJumpsInRoute = target.RemainingJumpsInRoute;
            NextSystemName = target.Name;
            NextStarClass = target.StarClass;
        }

        void AssignFSDJump(FSDJump jump)
        {
            ScanPercent = 0;
            ScannedBodies.Clear();
            BodySignals.Clear();
        }

    }
}
