using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class LoadGameEventHandler : BaseEventHandler, IJournalEventHandler<LoadGame>
    {
        public void HandleEvent(LoadGame journal)
        {
            GameState.Assign(journal);

            var log = new BridgeLog(journal);
            log.SpokenOnly();

            var shipName = GameState.ShipName;
            if (!String.IsNullOrEmpty(shipName) && !shipName.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
                shipName = "the " + shipName;

            log.DetailSsml.Append($"Welcome Commander")
                    .AppendEmphasis(GameState.Commander + ",", Framework.EmphasisType.Moderate)
                    .Append("flying")
                    .AppendEmphasis(shipName, Framework.EmphasisType.Moderate)
                    .Append("with")
                    .AppendNumber(GameState.Credits)
                    .Append("credits");

            log.Send();
        }
    }
}
