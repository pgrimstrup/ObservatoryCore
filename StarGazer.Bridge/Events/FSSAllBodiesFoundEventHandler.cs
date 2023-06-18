using Observatory.Framework.Files.Journal;
using StarGazer.Framework;

namespace StarGazer.Bridge.Events
{
    internal class FSSAllBodiesFoundEventHandler : BaseEventHandler, IJournalEventHandler<FSSAllBodiesFound>
    {
        public void HandleEvent(FSSAllBodiesFound journal)
        {
            if (GameState.ScanPercent == 100)
                return;

            SendScanComplete(journal);
            GameState.ScanPercent = 100;
        }

    }
}
