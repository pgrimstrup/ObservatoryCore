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
            Bridge.Instance.CommanderName = journal.Commander;
            Bridge.Instance.ShipType = journal.Ship;
            Bridge.Instance.ShipName = journal.ShipName;
            Bridge.Instance.Credits = journal.Credits;

            var log = new BridgeLog(journal);
            log.SpokenOnly();

            if(journal.ShipName.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
                log.DetailSsml.Append($"Welcome Commander {journal.Commander}, flying {journal.ShipName} with");
            else
                log.DetailSsml.Append($"Welcome Commander {journal.Commander}, flying the {journal.ShipName} with");

            log.DetailSsml.AppendNumber(journal.Credits);
            log.DetailSsml.Append("credits");

            Bridge.Instance.LogEvent(log);
        }
    }
}
