﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;
using Observatory.Framework.Files.ParameterTypes;

namespace Observatory.Bridge.Events
{
    internal class DockingDeniedEventHandler : BaseEventHandler, IJournalEventHandler<DockingDenied>
    {
        public void HandleEvent(DockingDenied journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.Append($"{journal.StationName} Tower has denied our docking request.");

            switch (journal.Reason)
            {
                case Reason.TooLarge:
                    log.DetailSsml.Append("Our ship is too large for their landing pads Commander.");
                    break;
                case Reason.Offences:
                    log.DetailSsml.Append("Apparently we have outstanding offences against them Commander. We might want to rectify that first.");
                    break;
                case Reason.DockOffline:
                    log.DetailSsml.Append("Their docking system is offline Commander. We may have to do this one manually.");
                    break;
                case Reason.ActiveFighter:
                    log.DetailSsml.Append("We have an active fighter in flight Commander. We better bring them back on board first.");
                    break;
                case Reason.Distance:
                    log.DetailSsml.Append("We made the request a bit early Commander. Let's close to within 7.5 kilometers and try to resubmit.");
                    break;
                case Reason.NoSpace:
                    log.DetailSsml.Append("Sorry Commander. No room at the inn. All landing pads are occupied.");
                    break;
                case Reason.RestrictedAccess:
                    log.DetailSsml.Append("Looks like access is restricted Commander.");
                    break;
                default:
                    log.DetailSsml.Append("No specific reason given. Guess they don't like us Commander.");
                    break;
            }

            Bridge.Instance.LogEvent(log);
        }
    }
}
