using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class LaunchSRVEventHandler : BaseEventHandler, IJournalEventHandler<LaunchSRV>
    {
        public void HandleEvent(LaunchSRV journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Away Team");
            log.DetailSsml.Append($"Deploying {journal.SRVType_Localised} with {journal.Loadout} load-out").AppendEmphasis("Commander.", EmphasisType.Moderate);

            Bridge.Instance.LogEvent(log);
        }
    }
}
