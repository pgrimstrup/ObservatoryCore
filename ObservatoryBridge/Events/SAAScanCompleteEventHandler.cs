using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class SAAScanCompleteEventHandler : BaseEventHandler, IJournalEventHandler<SAAScanComplete>
    {
        public void HandleEvent(SAAScanComplete journal)
        {
            var log = new BridgeLog(journal);
            log.IsTitleSpoken = true;
            log.TitleSsml.Append("Body").AppendBodyName(GetBodyName(journal.BodyName));

            log.DetailSsml.AppendUnspoken(Emojis.Probe);
            if (journal.ProbesUsed <= journal.EfficiencyTarget)
                log.DetailSsml.Append($"Surface Scan complete, with efficiency bonus, using only {journal.ProbesUsed} probes Commander.");
            else
                log.DetailSsml.Append($"Surface Scan complete using {journal.ProbesUsed} probes Commander.");

            if (journal.Mappers == null || journal.Mappers.Count == 0)
            {
                log.DetailSsml.AppendEmphasis("Commander,", EmphasisType.Moderate);
                log.DetailSsml.Append($"we are the first to map body");
                log.DetailSsml.AppendBodyName(GetBodyName(journal.BodyName));
            }
            else
            {
                log.DetailSsml.Append("Body").AppendBodyName(GetBodyName(journal.BodyName)).Append("is");
            }

            if (Bridge.Instance.CurrentSystem.ScannedBodies.TryGetValue(journal.BodyName, out Scan? scan))
            {
                string article = "a ";
                string terraformable = "";
                if (!String.IsNullOrEmpty(scan.TerraformState))
                    terraformable = "terraformable ";
                else if (scan.PlanetClass.IndexOfAny("aeiou".ToCharArray()) == 0)
                    article = "an ";

                log.DetailSsml.Append($", {article}{terraformable}");
                log.DetailSsml.AppendBodyType(scan.PlanetClass);
            }

            Bridge.Instance.LogEvent(log);
        }
    }
}
