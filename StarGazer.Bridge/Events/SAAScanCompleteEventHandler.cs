using Observatory.Framework.Files.Journal;
using StarGazer.Framework;

namespace StarGazer.Bridge.Events
{
    internal class SAAScanCompleteEventHandler : BaseEventHandler, IJournalEventHandler<SAAScanComplete>
    {
        public void HandleEvent(SAAScanComplete journal)
        {
            var spokenOnly = new BridgeLog(journal);
            spokenOnly.SpokenOnly();
            spokenOnly.TitleSsml.AppendBodyName(GetBodyName(journal.BodyName));

            if (journal.ProbesUsed <= journal.EfficiencyTarget)
            {
                spokenOnly.DetailSsml
                    .Append($"Surface Scan complete, with efficiency bonus, using only {journal.ProbesUsed} probes.");
            }
            else
            {
                spokenOnly.DetailSsml
                    .Append($"Surface Scan complete using {journal.ProbesUsed} probes.");
            }

            if (GameState.ScannedBodies.TryGetValue(journal.BodyName, out Scan? scan))
            {
                if(scan.WasMapped)
                {
                    spokenOnly.DetailSsml.Append($"We've mapped");
                }
                else
                {
                    spokenOnly.DetailSsml.AppendEmphasis("Commander,", EmphasisType.Moderate);
                    spokenOnly.DetailSsml.Append($"we are the first to map");
                }

                spokenOnly.DetailSsml
                    .AppendBodyName(GetBodyName(journal.BodyName))
                    .Append(",");

                if (scan.Landable)
                {
                    spokenOnly.DetailSsml.Append("a landable");
                    if (!String.IsNullOrWhiteSpace(scan.TerraformState))
                        spokenOnly.DetailSsml.Append(scan.TerraformState);
                }
                else if (!String.IsNullOrWhiteSpace(scan.TerraformState))
                {
                    spokenOnly.DetailSsml.Append(ArticleFor(scan.TerraformState)).Append(scan.TerraformState);
                }
                else
                {
                    spokenOnly.DetailSsml.Append(ArticleFor(scan.PlanetClass));
                }

                spokenOnly.DetailSsml.AppendBodyType(scan.PlanetClass);


                if (!String.IsNullOrWhiteSpace(scan.Atmosphere))
                    spokenOnly.DetailSsml.Append("with " + scan.Atmosphere);
                else if(scan.Landable)
                    spokenOnly.DetailSsml.Append("with no atmosphere");

                double gravity = Math.Round(scan.SurfaceGravity / 9.804, 2);
                spokenOnly.DetailSsml.Append($"and surface gravity {gravity} G.");
            }

            var entry = FindLogEntry(nameof(Scan), GetBodyName(journal.BodyName));
            if(entry != null && GameState.ScannedBodies.TryGetValue(journal.BodyName, out var scanned))
            {
                var k_value = BodyValueEstimator.GetKValueForBody(scanned.PlanetClass, !String.IsNullOrEmpty(scanned.TerraformState));
                var currentValue = BodyValueEstimator.GetBodyValue(k_value, scanned.MassEM, !scanned.WasDiscovered, true, !scanned.WasMapped, journal.ProbesUsed <= journal.EfficiencyTarget);
                var mappedValue = BodyValueEstimator.GetBodyValue(k_value, scanned.MassEM, !scanned.WasDiscovered, true, !scanned.WasMapped, true);

                entry.Mapped = Emojis.Mapped + $" {journal.ProbesUsed}/{journal.EfficiencyTarget}";
                entry.CurrentValue = $"{currentValue:n0} Cr";
                entry.MappedValue = $"{mappedValue:n0} Cr";
            }

            spokenOnly.Send();
        }
    }
}
