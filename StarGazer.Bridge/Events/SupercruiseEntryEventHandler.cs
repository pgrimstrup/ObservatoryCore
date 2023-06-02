using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class SupercruiseEntryEventHandler : BaseEventHandler, IJournalEventHandler<SupercruiseEntry>
    {
        public void HandleEvent(SupercruiseEntry journal)
        {
            var log = new BridgeLog(journal);
            log.SpokenOnly();
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.Append($"FSD online, supercruising");
            log.Send();
        }
    }
}
