using Observatory.Framework.Files.Journal;
using StarGazer.Framework;

namespace StarGazer.Bridge.Events
{
    internal class DockingGrantedEventHandler : BaseEventHandler, IJournalEventHandler<DockingGranted>
    {
        public void HandleEvent(DockingGranted journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");

            string stationName = journal.StationName + " Tower";
            if (journal.StationType == "FleetCarrier")
                if (TryGetStationName(journal.StationName, out var name))
                    stationName = name + " Flight";

            log.DetailSsml
                .Append($"{stationName}, confirmed docking on landing pad")
                .AppendEmphasis(journal.LandingPad.ToString(), EmphasisType.Moderate)
                .EndSentence();

            if(journal.StationType == "Orbis" || journal.StationType == "Coriolis" || journal.StationType == "Ocellus")
                if (DockingPads.PadLocations.TryGetValue(journal.LandingPad, out var location))
                    log.DetailSsml.Append($"Landing pad {journal.LandingPad} is located at {location}.");

            Bridge.Instance.LogEvent(log);
        }
    }
}
