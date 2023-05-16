using Observatory.Framework.Files.Journal;
using StarGazer.Framework;

namespace StarGazer.Bridge.Events
{
    internal class LaunchSRVEventHandler : BaseEventHandler, IJournalEventHandler<LaunchSRV>
    {
        public void HandleEvent(LaunchSRV journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Away Team");
            log.DetailSsml
                .Append($"{journal.SRVType_Localised} deployed with {journal.Loadout} load-out")
                .AppendEmphasis("Commander.", EmphasisType.Moderate);

            Bridge.Instance.LogEvent(log);
        }
    }
}
