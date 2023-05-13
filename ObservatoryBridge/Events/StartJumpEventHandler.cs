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
            LogInfo($"StartJump: {journal.Event} to {journal.StarSystem} (star class {journal.StarClass})");

            // We get this event when entering supercruise if we have a destination locked
            if (!String.IsNullOrEmpty(journal.StarSystem))
            {
                var log = new BridgeLog(journal);
                if (Bridge.Instance.CurrentSystem.NextDestinationNotify > DateTime.Now)
                    log.TextOnly();

                log.TitleSsml.Append("Flight Operations");

                var scoopable = ScoopableStars.Contains(journal.StarClass) ? ", scoopable" : ", non-scoopable";
                log.DetailSsml
                    .Append("Jumping to")
                        .AppendBodyName(journal.StarSystem)
                        .Append($". Destination star is a")
                        .AppendBodyType(GetStarTypeName(journal.StarClass))
                        .Append($"{scoopable}.");

                if (Bridge.Instance.CurrentSystem.RemainingJumpsInRoute > 0 && (Bridge.Instance.CurrentSystem.RemainingJumpsInRoute < 5 || (Bridge.Instance.CurrentSystem.RemainingJumpsInRoute % 5) == 0))
                    log.DetailSsml.Append($"There are {Bridge.Instance.CurrentSystem.RemainingJumpsInRoute} jumps remaining in the current flight plan.");

                Bridge.Instance.LogEvent(log);
                Bridge.Instance.CurrentSystem.NextDestinationNotify = DateTime.Now.Add(SpokenDestinationInterval); // notify in 30 seconds time
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
