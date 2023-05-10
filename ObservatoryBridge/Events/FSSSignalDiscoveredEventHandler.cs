using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class FSSSignalDiscoveredEventHandler : BaseEventHandler, IJournalEventHandler<FSSSignalDiscovered>
    {
        public void HandleEvent(FSSSignalDiscovered journal)
        {
            if (!String.IsNullOrEmpty(journal.USSType_Localised))
            {
                var log = new BridgeLog(journal);
                log.TitleSsml.Append("Science Station");

                log.DetailSsml.Append($"Sensors are picking up {journal.USSType_Localised} signal Commander. Threat level {journal.ThreatLevel}.");
                var minutes = (int)Math.Truncate(journal.TimeRemaining) / 60;
                var seconds = (int)Math.Truncate(journal.TimeRemaining) % 60;
                if (minutes > 0 || seconds > 0)
                {
                    log.DetailSsml.Append($"{minutes} " + (minutes == 1 ? "minute" : "minutes"));
                    log.DetailSsml.Append($"and {seconds} " + (seconds == 1 ? "second" : "seconds") + " remaining.");
                }
                Bridge.Instance.LogEvent(log);
            }
        }
    }
}
