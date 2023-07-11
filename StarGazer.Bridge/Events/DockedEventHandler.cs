using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class DockedEventHandler : BaseEventHandler, IJournalEventHandler<Docked>
    {
        public void HandleEvent(Docked journal)
        {
            if(GameState.Carriers.TryGetValue(journal.StationName, out string? carrierName))
                GameState.CurrentLocation.Set(0, carrierName + " " + journal.StationName);
            else
                GameState.CurrentLocation.Set(0, journal.StationName);

            string stationName = journal.StationName + " Tower";
            if (journal.StationType == "FleetCarrier")
                stationName = carrierName + " Flight";

            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.Append($"{stationName}, we have completed docking.");

            Bridge.Instance.LogEvent(log);
        }
    }
}
