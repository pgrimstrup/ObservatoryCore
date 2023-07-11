using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class FSDJumpEventHandler : BaseEventHandler, IJournalEventHandler<FSDJump>
    {
        public void HandleEvent(FSDJump journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");

            string arrivedAt = "Arrived at";
            if (journal.SystemAddress == GameState.RouteDestination.SystemAddress)
                arrivedAt = "We have reached our destination, system";

            log.DetailSsml
                .Append($"Jump completed")
                .AppendEmphasis("Commander.", Framework.EmphasisType.Moderate)
                .Append(arrivedAt)
                .AppendBodyName(journal.StarSystem)
                .Append(". We travelled")
                .AppendNumber(Math.Round(journal.JumpDist, 2))
                .Append("light years, using")
                .AppendNumber(Math.Round(journal.FuelUsed, 2))
            .Append("tons of fuel.");

            Bridge.Instance.ResetLogEntries();

            log.Send();

            // Next time we prepare or start a jump, we need to speak the destination
            GameState.DestinationTimeToSpeak = DateTime.Now;
        }
    }
}
