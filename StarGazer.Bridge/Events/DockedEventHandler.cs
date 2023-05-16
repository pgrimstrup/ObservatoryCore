using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class DockedEventHandler : BaseEventHandler, IJournalEventHandler<Docked>
    {
        public void HandleEvent(Docked journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.Append($"{journal.StationName} Tower, we have completed docking.");

            Bridge.Instance.LogEvent(log);
        }
    }
}
