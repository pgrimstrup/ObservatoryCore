using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class ApproachBodyEventHandler : BaseEventHandler, IJournalEventHandler<ApproachBody>
    {
        public void HandleEvent(ApproachBody journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");

            log.DetailSsml.AppendUnspoken(Emojis.Approaching);
            log.DetailSsml.Append($"On approach to")
                .AppendBodyName(GetBodyName(journal.Body));

            Bridge.Instance.LogEvent(log);
        }
    }
}
