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
            if (!String.IsNullOrEmpty(journal.StarSystem) && Bridge.Instance.CurrentSystem.CoursePlotted != journal.StarSystem)
            {
                LogInfo($"FSDStartJump: {journal.Event} to {journal.StarSystem} (star class {journal.StarClass})");

                var log = new BridgeLog(journal);
                log.TitleSsml.Append("Flight Operations");

                var scoopable = ScoopableStars.Contains(journal.StarClass) ? ", scoopable" : ", non-scoopable";
                log.DetailSsml
                    .Append("Jumping to")
                    .AppendBodyName(journal.StarSystem)
                    .Append($". Destination star is type {journal.StarClass}{scoopable}.");

                Bridge.Instance.LogEvent(log);
            }

            if (journal.StarClass.IsNeutronStar() || journal.StarClass.IsWhiteDwarf())
            {
                var log = new BridgeLog(journal);
                log.SpokenOnly();

                log.DetailSsml.AppendEmphasis("Commander,", EmphasisType.Moderate);
                log.DetailSsml.Append("this is a dangerous star type.");
                log.DetailSsml.AppendEmphasis("Throttle down now.", EmphasisType.Strong);
                Bridge.Instance.LogEvent(log);
            }
        }
    }
}
