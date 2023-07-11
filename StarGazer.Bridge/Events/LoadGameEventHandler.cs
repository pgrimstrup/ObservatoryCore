using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class LoadGameEventHandler : BaseEventHandler, IJournalEventHandler<LoadGame>
    {
        public void HandleEvent(LoadGame journal)
        {
            var log = new BridgeLog(journal);
            log.SpokenOnly();

            var shipName = GameState.ShipName;
            if (!String.IsNullOrEmpty(shipName) && !shipName.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
                shipName = "the " + shipName;

            log.DetailSsml.Append($"Welcome Commander")
                    .AppendEmphasis(GameState.Commander + ",", Framework.EmphasisType.Moderate)
                    .Append("flying the " + journal.Ship_Localised)
                    .AppendEmphasis(shipName, Framework.EmphasisType.Moderate)
                    .Append("with")
                    .AppendNumber(GameState.Credits)
                    .Append("credits.");

            switch (journal.GameMode)
            {
                case "Solo":
                    log.DetailSsml.Append("You have connected in Solo mode.");
                    break;

                case "Group":
                    log.DetailSsml.Append("You have connected to the private group")
                        .Append(journal.Group);
                    break;

                case "Open":
                    log.DetailSsml.Append("You have connected in Open mode. Take care around engineer systems.");
                    break;
            }

            log.Send();
        }
    }
}
