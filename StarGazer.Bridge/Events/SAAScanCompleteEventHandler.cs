using Observatory.Framework.Files.Journal;
using StarGazer.Framework;

namespace StarGazer.Bridge.Events
{
    internal class SAAScanCompleteEventHandler : BaseEventHandler, IJournalEventHandler<SAAScanComplete>
    {
        public void HandleEvent(SAAScanComplete journal)
        {
            var log = new BridgeLog(journal);
            log.IsTitleSpoken = true;
            log.TitleSsml.AppendBodyName(GetBodyName(journal.BodyName));

            log.DetailSsml.AppendUnspoken(Emojis.Probe);
            if (journal.ProbesUsed <= journal.EfficiencyTarget)
                log.DetailSsml.Append($"Surface Scan complete, with efficiency bonus, using only {journal.ProbesUsed} probes")
                    .AppendEmphasis("Commander.", EmphasisType.Moderate);
            else
                log.DetailSsml.Append($"Surface Scan complete using {journal.ProbesUsed} probes")
                    .AppendEmphasis("Commander.", EmphasisType.Moderate);

            if (journal.Mappers == null || journal.Mappers.Count == 0)
            {
                log.DetailSsml.AppendEmphasis("Commander,", EmphasisType.Moderate);
                log.DetailSsml.Append($"we are the first to map");
                log.DetailSsml.AppendBodyName(GetBodyName(journal.BodyName));
            }
            else
            {
                log.DetailSsml.AppendBodyName(GetBodyName(journal.BodyName)).Append("is");
            }

            if (GameState.ScannedBodies.TryGetValue(journal.BodyName, out Scan? scan))
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

            foreach (BridgeLog entry in Bridge.Instance.Logs)
            {
                if (entry.EventName == nameof(Scan) && entry.Title == GetBodyName(journal.BodyName))
                {
                    entry.DetailSsml.InsertEmoji(Emojis.Mapped);
                }
            }

            Bridge.Instance.LogEvent(log);
        }
    }
}
