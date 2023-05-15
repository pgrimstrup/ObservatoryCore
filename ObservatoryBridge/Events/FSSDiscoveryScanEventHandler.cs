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
        public async void HandleEvent(FSSDiscoveryScan journal)
        {
            LogInfo($"{journal.Event}: {journal.BodyCount} bodies, {journal.NonBodyCount} non-bodies, {journal.Progress * 100:n0} percent");
            if(Bridge.Instance.CurrentSystem.ScanPercent == 100)
                return;

            Bridge.Instance.CurrentSystem.ScanPercent = (int)(journal.Progress * 100);

            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Science Station");

            string plural = journal.BodyCount == 1 ? "body" : "bodies";
            log.DetailSsml.Append($"Discovery scan found {journal.BodyCount} {plural}").AppendEmphasis("Commander.", EmphasisType.Moderate);
            log.DetailSsml.Append($"Progress is {journal.Progress * 100:n0} percent.");
            if (Bridge.Instance.CurrentSystem.ScanPercent == 100)
                log.DetailSsml.Append("All bodies found.");

            Bridge.Instance.LogEvent(log);
            await Task.CompletedTask;
        }
    }
}
