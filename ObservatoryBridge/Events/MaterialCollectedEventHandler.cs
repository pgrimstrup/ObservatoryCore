using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class MaterialCollectedEventHandler : BaseEventHandler, IJournalEventHandler<MaterialCollected>
    {
        public void HandleEvent(MaterialCollected journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Away Team");
            log.DetailSsml.Append($"Collected {journal.Count} units of {journal.Name_Localised}.");

            Bridge.Instance.LogEvent(log);
        }
    }
}
