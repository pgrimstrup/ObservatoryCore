using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class SupercruiseExitEventHandler : BaseEventHandler, IJournalEventHandler<SupercruiseExit>
    {
        public void HandleEvent(SupercruiseExit journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.Append($"Exiting super-cruise, sub-light engines active.");

            Bridge.Instance.LogEvent(log);
        }
    }
}
