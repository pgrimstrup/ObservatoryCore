﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class DockSRVEventHandler : BaseEventHandler, IJournalEventHandler<DockSRV>
    {
        public void HandleEvent(DockSRV journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Away Team");
            log.DetailSsml.Append($"{journal.SRVType_Localised} returned to SRV docking bay Commander.");

            Bridge.Instance.LogEvent(log);
        }
    }
}