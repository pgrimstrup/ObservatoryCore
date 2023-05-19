using Observatory.Framework.Files.Journal;
using StarGazer.Framework;

namespace StarGazer.Bridge.Events
{
    internal class ScanEventHandler : BaseEventHandler, IJournalEventHandler<Scan>
    {
        public void HandleEvent(Scan journal)
        {
            // Can be sent again after a DSS, so we simply add in a bit more detail
            if (GameState.ScannedBodies.ContainsKey(journal.BodyName))
                return;

            GameState.ScannedBodies.Add(journal.BodyName, journal);
            GameState.BodySignals.TryGetValue(journal.BodyName, out var signals);

            if (!String.IsNullOrEmpty(journal.StarType))
            {
                // Otherwise, stars are text-logged only and not spoken
                var log = new BridgeLog(journal);
                log.TextOnly();

                if (Int32.TryParse(GetBodyName(journal.BodyName), out int bodyNumber))
                    log.TitleSsml.AppendBodyName(GetBodyName(journal.BodyName));
                else
                    log.TitleSsml.AppendBodyName(GetBodyName(journal.BodyName));

                if (journal.StarType.IsBlackHole())
                    log.DetailSsml.AppendUnspoken(Emojis.BlackHole);
                else if (journal.StarType.IsWhiteDwarf())
                    log.DetailSsml.AppendUnspoken(Emojis.WhiteDwarf);
                else
                    log.DetailSsml.AppendUnspoken(Emojis.Solar);

                var fuelStar = journal.StarType.IsFuelStar() ? ", a fuel star" : "";
                log.DetailSsml
                    .AppendBodyType(GetStarTypeName(journal.StarType))
                    .Append($"{fuelStar}.");

                log.Send();
            }
            else if (!String.IsNullOrEmpty(journal.PlanetClass)) // ignore belt clusters
            {
                var k_value = BodyValueEstimator.GetKValueForBody(journal.PlanetClass, !String.IsNullOrEmpty(journal.TerraformState));
                var estimatedValue = BodyValueEstimator.GetBodyValue(k_value, journal.MassEM, !journal.WasDiscovered, true, !journal.WasMapped, true);
                int bioCount = 0;
                int geoCount = 0;

                if (signals != null)
                {
                    List<string> list = new List<string>();
                    foreach (var signal in signals.Signals)
                    {
                        list.Add($"{signal.Count} {signal.Type_Localised}");
                        if (signal.Type_Localised.StartsWith("Geo", StringComparison.OrdinalIgnoreCase))
                            geoCount += signal.Count;
                        if (signal.Type_Localised.StartsWith("Bio", StringComparison.OrdinalIgnoreCase))
                            bioCount += signal.Count;
                    }
                }

                List<string> emojies = new List<string>();
                if (journal.PlanetClass.IsEarthlike())
                    emojies.Add(Emojis.Earthlike);
                else if (journal.PlanetClass.IsWaterWorld())
                    emojies.Add(Emojis.WaterWorld);
                else if (journal.PlanetClass.IsHighMetalContent())
                    emojies.Add(Emojis.HighMetalContent);
                else if (journal.PlanetClass.IsIcyBody())
                    emojies.Add(Emojis.IcyBody);
                else if (journal.PlanetClass.IsGasGiant())
                    emojies.Add(Emojis.GasGiant);
                else if (journal.PlanetClass.IsAmmoniaWorld())
                    emojies.Add(Emojis.Ammonia);
                else
                    emojies.Add(Emojis.OtherBody);

                if (!String.IsNullOrEmpty(journal.TerraformState))
                    emojies.Add(Emojis.Terraformable);

                if (estimatedValue >= Bridge.Instance.Settings.HighValueBody)
                    emojies.Add(Emojis.HighValue);

                if (bioCount > 0)
                    emojies.Add(Emojis.BioSignals);
                if (geoCount > 0)
                    emojies.Add(Emojis.GeoSignals);


                var log = new BridgeLog(journal);
                log.IsTitleSpoken = true;
                log.TitleSsml.AppendBodyName(GetBodyName(journal.BodyName));

                if (emojies.Count > 0)
                    log.DetailSsml.AppendUnspoken(String.Join("", emojies));

                if (!String.IsNullOrEmpty(journal.TerraformState))
                    log.DetailSsml.Append($"{journal.TerraformState}");

                log.DetailSsml.AppendBodyType(journal.PlanetClass);

                if (journal.Landable)
                {
                    if (!String.IsNullOrEmpty(journal.Atmosphere))
                        log.DetailSsml.Append($", landable with {journal.Atmosphere}");
                    else
                        log.DetailSsml.Append(", landable no atmosphere");
                }
                log.DetailSsml.EndSentence();

                if (bioCount > 0 || geoCount > 0)
                {
                    log.DetailSsml.Append("Sensors found");
                    if (bioCount > 0)
                        log.DetailSsml.Append($"{bioCount} biological");
                    if (bioCount > 0 && geoCount > 0)
                        log.DetailSsml.Append("and");
                    if (geoCount > 0)
                        log.DetailSsml.Append($"{geoCount} geological");
                    log.DetailSsml.Append("signal".Plural(bioCount + geoCount));
                    log.DetailSsml.EndSentence();
                }

                if (estimatedValue >= Bridge.Instance.Settings.HighValueBody)
                {
                    log.DetailSsml.Append($"Estimated value");
                    log.DetailSsml.AppendNumber(estimatedValue);
                    log.DetailSsml.Append("credits.");
                }
                else
                {
                    log.DetailSsml.AppendUnspoken($"Estimated value {estimatedValue:n0} credits.");
                }

                log.Send();
            }

            if(GameState.AutoCompleteScanCount > 0 && GameState.ScannedBodies.Count == GameState.AutoCompleteScanCount)
            {
                CreateOrrery(out int starCount, out int planetCount, out Scan primaryStar);
                string stars = $"{starCount} {Stars(starCount)}";
                string andPlanets = "";
                if (planetCount > 0)
                    andPlanets = $" and {planetCount} {Planets(planetCount)}";

                var log = new BridgeLog(journal);
                log.TitleSsml.Append("Science Station");
                log.DetailSsml
                    .Append($"System Scan Complete")
                    .AppendEmphasis("Commander.", EmphasisType.Moderate);

                if (primaryStar.WasDiscovered)
                    log.DetailSsml.Append($"We've discovered {stars}{andPlanets}.");
                else
                    log.DetailSsml.Append($"We are the first to discover this system consisting of {stars}{andPlanets}.");

                log.Send();
            }
        }
    }
}
