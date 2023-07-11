using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class UndockedEventHandler : BaseEventHandler, IJournalEventHandler<Undocked>
    {
        public void HandleEvent(Undocked journal)
        {
            if (!TryGetStationName(journal.StationName, out string stationName))
                stationName = journal.StationName;

            if (journal.StationType == "FleetCarrier")
                stationName = stationName + " Flight";
            else
                stationName = stationName + " Tower";

            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.Append($"{stationName}, we have cleared the pad and are on the way out.");

            Bridge.Instance.LogEvent(log);
        }
    }
}
