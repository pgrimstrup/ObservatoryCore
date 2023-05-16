using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class SupercruiseEntryEventHandler : BaseEventHandler, IJournalEventHandler<SupercruiseEntry>
    {
        public void HandleEvent(SupercruiseEntry journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.Append($"Supercruise engaged");

            Bridge.Instance.LogEvent(log);
        }
    }
}
