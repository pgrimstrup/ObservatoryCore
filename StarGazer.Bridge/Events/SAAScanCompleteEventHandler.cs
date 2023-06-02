using Observatory.Framework.Files.Journal;
using StarGazer.Framework;

namespace StarGazer.Bridge.Events
{
    internal class SAAScanCompleteEventHandler : BaseEventHandler, IJournalEventHandler<SAAScanComplete>
    {
        public void HandleEvent(SAAScanComplete journal)
        {
            var textOnly = new BridgeLog(journal);
            var spokenOnly = new BridgeLog(journal);
            textOnly.TextOnly();
            spokenOnly.SpokenOnly();
            textOnly.TitleSsml.AppendBodyName(GetBodyName(journal.BodyName));
            spokenOnly.TitleSsml.AppendBodyName(GetBodyName(journal.BodyName));

            textOnly.DetailSsml.AppendUnspoken(Emojis.Probe);
            if (journal.ProbesUsed <= journal.EfficiencyTarget)
            {
                textOnly.DetailSsml
                    .Append($"Surface Scan complete, with efficiency bonus, using only {journal.ProbesUsed} probes.");
                spokenOnly.DetailSsml
                    .Append($"Surface Scan complete, with efficiency bonus, using only {journal.ProbesUsed} probes")
                    .AppendEmphasis("Commander.", EmphasisType.Moderate);
            }
            else
            {
                textOnly.DetailSsml
                    .Append($"Surface Scan complete using {journal.ProbesUsed} probes.");
                spokenOnly.DetailSsml
                    .Append($"Surface Scan complete using {journal.ProbesUsed} probes")
                    .AppendEmphasis("Commander.", EmphasisType.Moderate);
            }

            if (journal.Mappers == null || journal.Mappers.Count == 0)
            {
                spokenOnly.DetailSsml.AppendEmphasis("Commander,", EmphasisType.Moderate);
                spokenOnly.DetailSsml.Append($"we are the first to map");
                spokenOnly.DetailSsml.AppendBodyName(GetBodyName(journal.BodyName)).AppendBreak();
            }
            else
            {
                spokenOnly.DetailSsml.AppendBodyName(GetBodyName(journal.BodyName)).AppendBreak();
            }

            if (GameState.ScannedBodies.TryGetValue(journal.BodyName, out Scan? scan))
            {
                textOnly.DetailSsml
                    .AppendBodyName(GetBodyName(journal.BodyName))
                    .Append(",");

                if (!String.IsNullOrWhiteSpace(scan.TerraformState))
                {
                    textOnly.DetailSsml.Append(ArticleFor(scan.TerraformState)).Append(scan.TerraformState).AppendBodyType(scan.PlanetClass);
                    spokenOnly.DetailSsml.Append(ArticleFor(scan.TerraformState)).Append(scan.TerraformState).AppendBodyType(scan.PlanetClass);
                }
                else
                {
                    textOnly.DetailSsml.Append(ArticleFor(scan.PlanetClass)).AppendBodyType(scan.PlanetClass);
                    spokenOnly.DetailSsml.Append(ArticleFor(scan.PlanetClass)).AppendBodyType(scan.PlanetClass);
                }

                if (scan.Landable)
                {
                    textOnly.DetailSsml.AppendBreak().Append("landable,");
                    spokenOnly.DetailSsml.AppendBreak().Append("landable");
                }

                if (String.IsNullOrWhiteSpace(scan.Atmosphere))
                {
                    textOnly.DetailSsml.Append("no atmosphere");
                    spokenOnly.DetailSsml.Append("with no atmosphere");
                }
                else
                {
                    textOnly.DetailSsml.Append(scan.Atmosphere);
                    spokenOnly.DetailSsml.Append("with " + scan.Atmosphere);
                }

                double gravity = Math.Round(scan.SurfaceGravity / 9.804, 2);
                textOnly.DetailSsml.Append($", surface gravity {gravity} G.");
                spokenOnly.DetailSsml.Append($"and surface gravity {gravity} G.");
            }
            else
            {
                spokenOnly.DetailSsml.Append("hmm. Interesting, this body doesn't appear in our system scan.");
            }

            foreach (BridgeLog entry in Bridge.Instance.Logs)
            {
                if (entry.EventName == nameof(Scan) && entry.Title == GetBodyName(journal.BodyName))
                {
                    entry.DetailSsml.InsertEmoji(Emojis.Mapped);
                }
            }

            Bridge.Instance.LogEvent(textOnly);
            Bridge.Instance.LogEvent(spokenOnly);
        }
    }
}
