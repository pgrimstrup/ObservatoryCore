using Observatory.Framework.Files.Journal;
using StarGazer.Framework;

namespace StarGazer.Bridge.Events
{
    internal class FSSDiscoveryScanEventHandler : BaseEventHandler, IJournalEventHandler<FSSDiscoveryScan>
    {
        public async void HandleEvent(FSSDiscoveryScan journal)
        {
            LogInfo($"{journal.Event}: {journal.BodyCount} bodies, {journal.NonBodyCount} non-bodies, {journal.Progress * 100:n0} percent");
            if(GameState.ScanPercent == 100)
                return;

            GameState.ScanPercent = (int)(journal.Progress * 100);

            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Science Station");

            string plural = journal.BodyCount == 1 ? "body" : "bodies";
            log.DetailSsml.Append($"Discovery scan found {journal.BodyCount} {plural}").AppendEmphasis("Commander.", EmphasisType.Moderate);
            log.DetailSsml.Append($"Progress is {journal.Progress * 100:n0} percent.");
            if (GameState.ScanPercent == 100)
                log.DetailSsml.Append("All bodies found.");

            Bridge.Instance.LogEvent(log);
            await Task.CompletedTask;
        }
    }
}
