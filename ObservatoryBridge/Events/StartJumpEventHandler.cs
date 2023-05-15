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
                LogInfo($"StartJump: {journal.Event} to {journal.StarSystem} (star class {journal.StarClass})");

                var log = new BridgeLog(journal);
                if (Bridge.Instance.CurrentSystem.NextDestinationNotify > DateTime.Now)
                    log.TextOnly();

                log.TitleSsml.Append("Flight Operations");

                var scoopable = journal.StarClass.IsScoopable() ? ", scoopable" : ", non-scoopable";
                log.DetailSsml
                    .Append("Jumping to")
                        .AppendBodyName(journal.StarSystem)
                        .Append($". Destination star is a")
                        .AppendBodyType(GetStarTypeName(journal.StarClass))
                        .Append($"{scoopable}.");

                if (Bridge.Instance.CurrentSystem.RemainingJumpsInRoute > 0 && (Bridge.Instance.CurrentSystem.RemainingJumpsInRoute < 5 || (Bridge.Instance.CurrentSystem.RemainingJumpsInRoute % 5) == 0))
                    log.DetailSsml.Append($"There are {Bridge.Instance.CurrentSystem.RemainingJumpsInRoute} jumps remaining in the current flight plan.");

                Bridge.Instance.LogEvent(log);
                if (!Bridge.Instance.Core.IsLogMonitorBatchReading)
                    Bridge.Instance.CurrentSystem.NextDestinationNotify = DateTime.Now.Add(SpokenDestinationInterval);


                if (journal.StarClass.IsNeutronStar() || journal.StarClass.IsWhiteDwarf())
                {
                    log = new BridgeLog(journal);
                    log.SpokenOnly();

                    log.DetailSsml.AppendEmphasis("Commander,", EmphasisType.Moderate);
                    log.DetailSsml.Append("this is a dangerous star type.");
                    log.DetailSsml.AppendEmphasis("Throttle down now.", EmphasisType.Strong);
                    Bridge.Instance.LogEvent(log);
                }
            }
        }
    }
}
