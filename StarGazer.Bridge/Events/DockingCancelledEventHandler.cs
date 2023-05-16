using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class DockingCancelledEventHandler : BaseEventHandler, IJournalEventHandler<DockingCancelled>
    {
        public void HandleEvent(DockingCancelled journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.Append($"{journal.StationName} Tower has cancelled our docking request")
                .AppendEmphasis("Commander.", Framework.EmphasisType.Moderate)
                .Append("We'll need to resubmit another request if we want to dock.");

            Bridge.Instance.LogEvent(log);
        }
    }
}
