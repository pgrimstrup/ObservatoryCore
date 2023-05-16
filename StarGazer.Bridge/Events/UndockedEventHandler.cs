using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class UndockedEventHandler : BaseEventHandler, IJournalEventHandler<Undocked>
    {
        public void HandleEvent(Undocked journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.Append($"{journal.StationName} Tower, we have cleared the pad and are on the way out.");

            Bridge.Instance.LogEvent(log);
        }
    }
}
