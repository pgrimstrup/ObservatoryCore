using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class FSSAllBodiesFoundEventHandler : BaseEventHandler, IJournalEventHandler<FSSAllBodiesFound>
    {
        public void HandleEvent(FSSAllBodiesFound journal)
        {
            if (Bridge.Instance.CurrentSystem.ScanPercent == 100)
                return;

            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Science Station");
            log.DetailSsml.Append($"System Scan Complete")
                .AppendEmphasis("Commander.", EmphasisType.Moderate)
                .Append("We've found all bodies in this system.");

            Bridge.Instance.LogEvent(log);
            Bridge.Instance.CurrentSystem.ScanPercent = 100;
        }
    }
}
