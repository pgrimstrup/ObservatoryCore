using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework;
using Observatory.Framework.Files;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class FSDTargetEventHandler : BaseEventHandler, IJournalEventHandler<FSDTarget>
    {
        public void HandleEvent(FSDTarget journal)
        {
            LogInfo($"FSDTarget: {journal.Event} to {journal.Name} ({journal.StarClass})");
            Bridge.Instance.CurrentSystem.Assign(journal);

            // This event is received mid-jump when auto-plotting to the next star system in the route
            if (Bridge.Instance.CurrentShip.Status.HasFlag(Framework.Files.ParameterTypes.StatusFlags.FSDJump))
                return;

            // Manually selected the next destination system
            if (!String.IsNullOrEmpty(journal.Name))
            {
                var log = new BridgeLog(journal);
                log.TitleSsml.Append("Flight Operations");

                var scoopable = ScoopableStars.Contains(journal.StarClass) ? ", scoopable" : ", non-scoopable";
                log.DetailSsml
                    .Append("Course laid in to")
                        .AppendBodyName(journal.Name)
                        .Append($". Destination star is a")
                        .AppendBodyType(GetStarTypeName(journal.StarClass))
                        .Append($"{scoopable}.");

                if (Bridge.Instance.CurrentSystem.RemainingJumpsInRoute > 0 && (Bridge.Instance.CurrentSystem.RemainingJumpsInRoute < 5 || (Bridge.Instance.CurrentSystem.RemainingJumpsInRoute % 5) == 0))
                    log.DetailSsml.Append($"There are {Bridge.Instance.CurrentSystem.RemainingJumpsInRoute} jumps remaining in the current flight plan.");

                if (journal.StarClass.IsNeutronStar() || journal.StarClass.IsWhiteDwarf() || journal.StarClass.IsBlackHole())
                {
                    log.DetailSsml
                        .AppendEmphasis("Commander,", EmphasisType.Moderate)
                        .Append("this is a hazardous star type.")
                        .AppendEmphasis("Caution is advised", EmphasisType.Strong)
                        .Append("on exiting jump");
                }

                Bridge.Instance.LogEvent(log);
                Bridge.Instance.CurrentSystem.NextDestinationNotify = DateTime.Now.Add(SpokenDestinationInterval);
            }
        }
    }
}
