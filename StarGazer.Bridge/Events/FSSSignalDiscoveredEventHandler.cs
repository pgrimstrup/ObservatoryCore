using Observatory.Framework.Files.Journal;
using StarGazer.Framework;

namespace StarGazer.Bridge.Events
{
    internal class FSSSignalDiscoveredEventHandler : BaseEventHandler, IJournalEventHandler<FSSSignalDiscovered>
    {
        public void HandleEvent(FSSSignalDiscovered journal)
        {
            // Track all station names in this system
            if (journal.IsStation)
            {
                var match = CarrierNameRegex.Match(journal.SignalNameSpoken);
                if(match.Success)
                {
                    GameState.Carriers[match.Groups[2].Value] = match.Groups[1].Value.Trim();
                }
                else
                {
                    if(!GameState.Stations.Contains(journal.SignalNameSpoken))
                        GameState.Stations.Add(journal.SignalNameSpoken);
                }
            }

            if (!String.IsNullOrEmpty(journal.USSTypeSpoken))
            {
                var log = new BridgeLog(journal);
                log.SpokenOnly();
                log.TitleSsml.Append("Science Station");

                log.DetailSsml.Append($"{journal.USSTypeSpoken},")
                    .Append($"threat level {journal.ThreatLevel}.");

                var minutes = (int)Math.Truncate(journal.TimeRemaining) / 60;
                var seconds = (int)Math.Truncate(journal.TimeRemaining) % 60;
                if(minutes == 0)
                {
                    log.DetailSsml.Append("Less than a minute remaining.");
                }
                else 
                {
                    log.DetailSsml.Append(BridgeUtils.CountAndPlural("minute", minutes));
                    log.DetailSsml.Append("remaining");
                }
                log.Send();
            }
        }
    }
}
