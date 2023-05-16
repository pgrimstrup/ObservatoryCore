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

            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Science Station");
            log.DetailSsml.Append($"System Scan Complete")
                .AppendEmphasis("Commander.", EmphasisType.Moderate)
                .Append("We've found all bodies in this system.");

            log.Send();
            GameState.ScanPercent = 100;
        }
    }
}
