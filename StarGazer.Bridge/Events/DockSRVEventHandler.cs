using Observatory.Framework.Files.Journal;
using StarGazer.Framework;

namespace StarGazer.Bridge.Events
{
    internal class DockSRVEventHandler : BaseEventHandler, IJournalEventHandler<DockSRV>
    {
        public void HandleEvent(DockSRV journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Away Team");
            log.DetailSsml.Append($"{journal.SRVType_Localised} returned to docking bay")
                .AppendEmphasis("Commander.", EmphasisType.Moderate);

            Bridge.Instance.LogEvent(log);
        }
    }
}
