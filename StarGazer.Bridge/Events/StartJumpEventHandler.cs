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

                AppendRemainingJumps(log, false);
                AppendHazardousStarWarning(log, journal.StarClass);

                log.Send();
                GameState.DestinationTimeToSpeak = DateTime.Now.Add(SpokenDestinationInterval);

                if (!log.IsSpoken)
                {
                    // Tell the user we are jumping while the countdown is running at least
                    log = new BridgeLog(journal);
                    log.SpokenOnly();
                    log.DetailSsml.Append("FSD Online, jumping.");
                    AppendHazardousStarWarning(log, journal.StarClass);
                    log.Send();
                }
            }
        }
    }
}
