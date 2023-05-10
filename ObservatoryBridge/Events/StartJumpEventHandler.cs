using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class StartJumpEventHandler : BaseEventHandler, IJournalEventHandler<StartJump>
    {
        public void HandleEvent(StartJump journal)
        {
            // We get this event when entering supercruise if we have a destination locked
            if (!String.IsNullOrEmpty(journal.StarSystem))
            {
                var log = new BridgeLog(journal);
                log.TitleSsml.Append("Flight Operations");

                var scoopable = ScoopableStars.Contains(journal.StarClass) ? ", scoopable" : ", non-scoopable";
                log.DetailSsml.Append($"Destination star is type {journal.StarClass}{scoopable}.");

                if (journal.StarClass.IsNeutronStar() || journal.StarClass.IsWhiteDwarf())
                {
                    log.DetailSsml.AppendEmphasis("Commander,", EmphasisType.Moderate);
                    log.DetailSsml.Append("this is a hazardous star type.");
                    log.DetailSsml.AppendEmphasis("Throttle down now.", EmphasisType.Strong);
                }

                Bridge.Instance.LogEvent(log);
            }
        }
    }
}
