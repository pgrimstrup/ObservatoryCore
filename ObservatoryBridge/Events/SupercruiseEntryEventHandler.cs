using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class SupercruiseEntryEventHandler : BaseEventHandler, IJournalEventHandler<SupercruiseEntry>
    {
        public void HandleEvent(SupercruiseEntry journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.Append($"Super-cruising, FSD active.");

            Bridge.Instance.LogEvent(log);
        }
    }
}
