using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class FuelScoopEventHandler : BaseEventHandler, IJournalEventHandler<FuelScoop>
    {
        public void HandleEvent(FuelScoop journal)
        {
            // Fuel Scooping Completed is slightly different to Fuel Scooping terminated.
            double total = Math.Round(journal.Total, 2);
            if (total >= GameState.FuelCapacity)
            {
                var log = new BridgeLog(journal);
                log.TitleSsml.Append("Fuel Scooping");

                log.DetailSsml.AppendUnspoken(Emojis.FuelScoop);
                log.DetailSsml
                    .Append($"Fuel scooping completed, collected")
                    .AppendNumber(Math.Round(GameState.FuelScooped, 2))
                    .Append("tons.");

                log.DetailSsml.Append("Main tank full.");

                log.Send();
                GameState.FuelScooped = 0;
            }
        }
    }
}
