using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class DockedEventHandler : BaseEventHandler, IJournalEventHandler<Docked>
    {
        public void HandleEvent(Docked journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.Append($"{journal.StationName} Tower, we have completed docking.");

            Bridge.Instance.LogEvent(log);
        }
    }
}
