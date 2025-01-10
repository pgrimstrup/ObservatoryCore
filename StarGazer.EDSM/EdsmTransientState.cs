using System.Dynamic;
using System.Text.Json;
using Observatory.Framework.Files.Journal;

namespace StarGazer.EDSM
{
    internal class EdsmTransientState
    {
        public bool IsCrew { get; set; } 
        public ulong? ShipId { get; set; }
        public string? ShipName { get; set; }
        public string? Commander { get; set; }
        public ulong? SystemAddress { get; set; }
        public string? SystemName { get; set; }
        public (double x, double y, double z)? SystemCoordinates { get; set; }
        public long? StationId { get; set; }
        public string? StationName { get; set; }

        
        public string? Software { get; set; }
        public string? SoftwareVersion { get; set; }
        public string? GameVersion { get; set; }
        public string? GameBuild { get; set; }

        public EdsmTransientState()
        {
            Software = GetType().Assembly.GetName().Name;
            SoftwareVersion = GetType().Assembly.GetName().Version?.ToString();
        }

        internal void ProcessJournalEvent(JournalBase journal) 
        {
            switch (journal)
            {
                case LoadGame load:
                    GameVersion = load.GameVersion;
                    GameBuild = load.Build;
                    Commander = load.Commander;
                    ShipId = load.ShipID;
                    ShipName = load.ShipName;
                    SystemAddress = null;
                    SystemCoordinates = null;
                    SystemName = null;
                    StationId = null;
                    StationName = null;
                    break;

                case SetUserShipName setUserShipName:
                    ShipId = setUserShipName.ShipID;
                    ShipName = setUserShipName.UserShipName;
                    break;

                case ShipyardBuy:
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

                case Undocked:
                    StationId = null;
                    StationName = null;
                    break;

                case Location location:
                    if(location.StarSystem != "ProvingGround" && location.StarSystem != "CQC")
                    {
                        SystemAddress = location.SystemAddress;
                        SystemName = location.StarSystem;
                        SystemCoordinates = location.StarPos;
                    }
                    else
                    {
                        SystemAddress = null;
                        SystemName = null;
                        SystemCoordinates = null;
                    }
                    StationId = location.MarketID;
                    StationName = location.StationName;
                    break;

                case FSDJump fsdJump:
                    if (fsdJump.StarSystem != "ProvingGround" && fsdJump.StarSystem != "CQC")
                    {
                        SystemAddress = fsdJump.SystemAddress;
                        SystemName = fsdJump.StarSystem;
                        SystemCoordinates = fsdJump.StarPos;
                    }
                    else
                    {
                        SystemAddress = null;
                        SystemName = null;
                        SystemCoordinates = null;
                    }
                    StationId = null;
                    StationName = null;
                    break;

                case Docked docked:
                    if (docked.StarSystem != "ProvingGround" && docked.StarSystem != "CQC")
                    {
                        SystemAddress = docked.SystemAddress;
                        SystemName = docked.StarSystem;
                    }
                    else
                    {
                        SystemAddress = null;
                        SystemName = null;
                    }
                    StationId = docked.MarketID;
                    StationName = docked.StationName;
                    break;

                case QuitACrew:
                    IsCrew = false;
                    SystemAddress = null;
                    SystemCoordinates = null;
                    SystemName = null;
                    StationId = null;
                    StationName = null;
                    break;

                case JoinACrew joinACrew:
                    IsCrew = joinACrew.Captain != Commander;
                    SystemAddress = null;
                    SystemCoordinates = null;
                    SystemName = null;
                    StationId = null;
                    StationName = null;
                    break;
            }
        }

        internal EdsmPayload CreatePayload(JournalBase journal)
        {
            EdsmPayload payload = new EdsmPayload();
            payload.Timestamp = journal.TimestampDateTime;
            payload.Event = journal.Event;

            dynamic obj = JsonSerializer.Deserialize<ExpandoObject>(journal.Json)!;
            obj._systemAddress = SystemAddress;
            obj._systemName = SystemName;
            obj._systemCoordinates = SystemCoordinates;
            obj._marketId = StationId;
            obj._stationName = StationName;
            obj._shipId = ShipId;
            obj._shipName = ShipName;

            payload.FromSoftware = Software;
            payload.FromSoftwareVersion = SoftwareVersion;
            payload.FromGameVersion = GameVersion;
            payload.FromGameBuild = GameBuild;
            payload.Message = obj;
            return payload;
        }
    }
}
