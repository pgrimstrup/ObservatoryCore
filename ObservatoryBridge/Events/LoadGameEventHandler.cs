using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class LoadGameEventHandler : BaseEventHandler, IJournalEventHandler<LoadGame>
    {
        public void HandleEvent(LoadGame journal)
        {
            Bridge.Instance.CurrentShip.Assign(journal);

            var log = new BridgeLog(journal);
            log.SpokenOnly();

            var shipName = Bridge.Instance.CurrentShip.ShipName;
            if (!shipName.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
                shipName = "the " + shipName;

            log.DetailSsml.Append($"Welcome Commander")
                    .AppendEmphasis(Bridge.Instance.CurrentShip.Commander + ",", Framework.EmphasisType.Moderate)
                    .Append("flying")
                    .AppendEmphasis(shipName, Framework.EmphasisType.Moderate)
                    .Append("with")
                    .AppendNumber(Bridge.Instance.CurrentShip.Credits)
                    .Append("credits");

            Bridge.Instance.LogEvent(log);
        }
    }
}
