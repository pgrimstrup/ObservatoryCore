using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class FSSDiscoveryScanEventHandler : BaseEventHandler, IJournalEventHandler<FSSDiscoveryScan>
    {
        public void HandleEvent(FSSDiscoveryScan journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Science Station");

            log.DetailSsml.Append($"Discovery scan found {journal.BodyCount} bodies").AppendEmphasis("Commander.", EmphasisType.Moderate);
            log.DetailSsml.Append($"Progress is {journal.Progress * 100:n0} percent.");

            Bridge.Instance.LogEvent(log);
        }
    }
}
