using Observatory.Framework.Files.Journal;
using StarGazer.Framework;

namespace StarGazer.Bridge.Events
{
    internal class StartJumpEventHandler : BaseEventHandler, IJournalEventHandler<StartJump>
    {
        public void HandleEvent(StartJump journal)
        {

            // We get this event when entering supercruise if we have a destination locked
            if (!String.IsNullOrWhiteSpace(journal.StarSystem))
            {
                var log = new BridgeLog(journal);
                if (GameState.DestinationTimeToSpeak > DateTime.Now)
                    log.TextOnly(); // Still need to log the event

                log.TitleSsml.Append("Flight Operations");

                var fuelStar = journal.StarClass.IsFuelStar() ? ", a fuel star" : "";
                log.DetailSsml
                    .Append("Jumping to")
                        .AppendBodyName(journal.StarSystem)
                        .Append($". Destination star is a")
                        .AppendBodyType(GetStarTypeName(journal.StarClass))
                        .Append($"{fuelStar}.");

                if (GameState.RemainingJumpsInRoute == 1)
                    log.DetailSsml.Append($"This is the final jump in the current flight plan.");
                else if (GameState.RemainingJumpsInRoute > 1 && GameState.RemainingJumpsInRouteTimeToSpeak < DateTime.Now && (GameState.RemainingJumpsInRoute < 5 || (GameState.RemainingJumpsInRoute % 5) == 0))
                {
                    log.DetailSsml.Append($"There are {GameState.RemainingJumpsInRoute} jumps remaining in the current flight plan.");
                    GameState.RemainingJumpsInRouteTimeToSpeak = DateTime.Now.Add(SpokenDestinationInterval * 2);
                }

                log.Send();
                if (!Bridge.Instance.Core.IsLogMonitorBatchReading)
                    GameState.DestinationTimeToSpeak = DateTime.Now.Add(SpokenDestinationInterval);

                if (!log.IsSpoken)
                {
                    // Tell the user we are jumping while the countdown is running at least
                    log = new BridgeLog(journal);
                    log.SpokenOnly();
                    log.DetailSsml.Append("FSD Online, jumping.");
                    log.Send();
                }

                if (journal.StarClass.IsNeutronStar() || journal.StarClass.IsWhiteDwarf() || journal.StarClass.IsBlackHole())
                {
                    log = new BridgeLog(journal);
                    log.SpokenOnly();

                    log.DetailSsml.AppendEmphasis("Commander,", EmphasisType.Moderate);
                    log.DetailSsml.Append("this is a dangerous star type.");
                    log.DetailSsml.AppendEmphasis("Throttle down now.", EmphasisType.Strong);
                    log.Send();
                }
            }
        }
    }
}
