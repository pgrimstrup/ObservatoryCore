using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class LeaveBodyEventHandler : BaseEventHandler, IJournalEventHandler<LeaveBody>
    {
        public void HandleEvent(LeaveBody journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");

            log.DetailSsml.AppendUnspoken(Emojis.Departing);
            log.DetailSsml.Append($"Departing")
                .AppendBodyName(GetBodyName(journal.Body));

            Bridge.Instance.LogEvent(log);
        }
    }
}
