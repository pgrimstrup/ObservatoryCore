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
            // This event is received mid-jump when auto-plotting to the next star system in the route
            if (Bridge.Instance.CurrentShip.Status.HasFlag(Framework.Files.ParameterTypes.StatusFlags.FSDJump))
            {
                Bridge.Instance.CurrentSystem.CoursePlotted = "";
                return;
            }

            if (!String.IsNullOrEmpty(journal.Name) && journal.Name != Bridge.Instance.CurrentSystem.CoursePlotted)
            {
                LogInfo($"FSDTarget: {journal.Event} to {journal.Name} ({journal.StarClass})");

                var log = new BridgeLog(journal);
                log.TitleSsml.Append("Flight Operations");

                var scoopable = ScoopableStars.Contains(journal.StarClass) ? ", scoopable" : ", non-scoopable";
                log.DetailSsml
                    .Append("Course laid in to")
                    .AppendBodyName(journal.Name)
                    .Append($". Destination star is type {journal.StarClass}{scoopable}.");

                if (journal.StarClass.IsNeutronStar() || journal.StarClass.IsWhiteDwarf())
                {
                    log.DetailSsml
                        .AppendEmphasis("Commander,", EmphasisType.Moderate)
                        .Append("this is a hazardous star type.")
                        .AppendEmphasis("Caution is advised", EmphasisType.Strong)
                        .Append("on exiting jump");
                }

                Bridge.Instance.LogEvent(log);
                Bridge.Instance.CurrentSystem.CoursePlotted = journal.Name;
            }
        }
    }
}
