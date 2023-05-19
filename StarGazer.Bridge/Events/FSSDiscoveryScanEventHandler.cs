using Observatory.Framework.Files.Journal;
using StarGazer.Framework;

namespace StarGazer.Bridge.Events
{
    internal class FSSDiscoveryScanEventHandler : BaseEventHandler, IJournalEventHandler<FSSDiscoveryScan>
    {
        public async void HandleEvent(FSSDiscoveryScan journal)
        {
            if(GameState.ScanPercent == 100)
                return;

            GameState.ScanPercent = (int)(journal.Progress * 100);
            if (GameState.ScanPercent == 100)
            {
                // Need to wait until all Scan events have been received
                GameState.AutoCompleteScanCount = journal.BodyCount;
            }
            else
            {
                // Indicate progress for partially discovered system
                var log = new BridgeLog(journal);
                log.TitleSsml.Append("Science Station");

                log.DetailSsml
                    .Append($"Discovery scan found {journal.BodyCount} {Bodies(journal.BodyCount)}")
                    .AppendEmphasis("Commander.", EmphasisType.Moderate)
                    .Append($"Progress is {journal.Progress * 100:n0} percent.");

                log.Send();
            }

            await Task.CompletedTask;
        }
    }
}
