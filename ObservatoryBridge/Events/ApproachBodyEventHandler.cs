using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class ApproachBodyEventHandler : BaseEventHandler, IJournalEventHandler<ApproachBody>
    {
        public void HandleEvent(ApproachBody journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");

            log.DetailSsml.AppendUnspoken(Emojis.Approaching);
            log.DetailSsml.Append($"On approach to body")
                .AppendBodyName(journal.Body);

            Bridge.Instance.LogEvent(log);
        }
    }
}
