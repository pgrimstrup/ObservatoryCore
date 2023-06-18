using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class LeaveBodyEventHandler : BaseEventHandler, IJournalEventHandler<LeaveBody>
    {
        public void HandleEvent(LeaveBody journal)
        {
            var log = new BridgeLog(journal);
            log.SpokenOnly();
            log.TitleSsml.Append("Flight Operations");

            log.DetailSsml.AppendUnspoken(Emojis.Departing);
            log.DetailSsml.Append($"Departing")
                .AppendBodyName(GetBodyName(journal.Body));

            Bridge.Instance.LogEvent(log);
        }
    }
}
