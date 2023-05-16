using Observatory.Framework.Files.Journal;

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
                LogInfo($"{journal.Event}: {journal.BodyName}, StarType {journal.StarType}");

                if(GetBodyName(journal.BodyName) == "Star A" && !journal.WasDiscovered && GameState.FirstDiscoverySpoken == DateTime.MinValue)
                {
                    var first = new BridgeLog(journal);
                    first.SpokenOnly().DetailSsml
                        .AppendEmphasis("Commander,", Framework.EmphasisType.Moderate)
                        .Append("we are the first to discover this system.");
                    Bridge.Instance.LogEvent(first);

                    if (!Bridge.Instance.Core.IsLogMonitorBatchReading)
                        GameState.FirstDiscoverySpoken = DateTime.Now;
                }

                var log = new BridgeLog(journal);
                log.IsTitleSpoken = true;

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

                var scoopable = journal.StarType.IsScoopable() ? ", scoopable" : ", non-scoopable";
                log.DetailSsml
                    .AppendBodyType(GetStarTypeName(journal.StarType))
                    .Append($"{scoopable}.");

                var estimatedValue = BodyValueEstimator.GetStarValue(journal.StarType, !journal.WasDiscovered);
                if (estimatedValue >= Bridge.Instance.Settings.HighValueBody)
                {
                    log.DetailSsml.AppendUnspoken(Emojis.HighValue);
                    log.DetailSsml.Append($"Estimated value");
                    log.DetailSsml.AppendNumber(estimatedValue);
                    log.DetailSsml.Append("credits.");
                }
                else
                {
                    log.DetailSsml.AppendUnspoken($"Estimated value {estimatedValue:n0} credits.");
                }

                Bridge.Instance.LogEvent(log);
            }
            else if (!String.IsNullOrEmpty(journal.PlanetClass)) // ignore belt clusters
            {
                LogInfo($"{journal.Event}: {journal.BodyName} is PlanetClass {journal.PlanetClass}");

                var log = new BridgeLog(journal);
                log.IsTitleSpoken = true;
                log.TitleSsml.AppendBodyName(GetBodyName(journal.BodyName));

                if (journal.PlanetClass.IsEarthlike())
                    log.DetailSsml.AppendUnspoken(Emojis.Earthlike);
                else if (journal.PlanetClass.IsWaterWorld())
                    log.DetailSsml.AppendUnspoken(Emojis.WaterWorld);
                else if (journal.PlanetClass.IsHighMetalContent())
                    log.DetailSsml.AppendUnspoken(Emojis.HighMetalContent);
                else if (journal.PlanetClass.IsIcyBody())
                    log.DetailSsml.AppendUnspoken(Emojis.IcyBody);
                else if (journal.PlanetClass.IsGasGiant())
                    log.DetailSsml.AppendUnspoken(Emojis.GasGiant);
                else if (journal.PlanetClass.IsAmmoniaWorld())
                    log.DetailSsml.AppendUnspoken(Emojis.Ammonia);
                else
                    log.DetailSsml.AppendUnspoken(Emojis.OtherBody);

                if (!String.IsNullOrEmpty(journal.TerraformState))
                    log.DetailSsml.AppendUnspoken(Emojis.Terraformable);

                if (!String.IsNullOrEmpty(journal.TerraformState))
                    log.DetailSsml.Append($"{journal.TerraformState}");

                log.DetailSsml.AppendBodyType(journal.PlanetClass);
                if (journal.Landable)
                {
                    if (!String.IsNullOrEmpty(journal.Atmosphere))
                        log.DetailSsml.Append($", landable with {journal.Atmosphere}.");
                    else
                        log.DetailSsml.Append(", landable no atmosphere.");
                }
                else
                    log.DetailSsml.EndSentence();

                var k_value = BodyValueEstimator.GetKValueForBody(journal.PlanetClass, !String.IsNullOrEmpty(journal.TerraformState));
                var estimatedValue = BodyValueEstimator.GetBodyValue(k_value, journal.MassEM, !journal.WasDiscovered, true, !journal.WasMapped, true);
                if (estimatedValue >= Bridge.Instance.Settings.HighValueBody)
                {
                    log.DetailSsml.AppendUnspoken(Emojis.HighValue);
                    log.DetailSsml.Append($"Estimated value");
                    log.DetailSsml.AppendNumber(estimatedValue);
                    log.DetailSsml.Append("credits.");
                }
                else
                {
                    log.DetailSsml.AppendUnspoken($"Estimated value {estimatedValue:n0} credits.");
                }

                if (signals != null)
                {
                    List<string> list = new List<string>();
                    bool hasBio = false;
                    bool hasGeo = false;
                    foreach (var signal in signals.Signals)
                    {
                        list.Add($"{signal.Count} {signal.Type_Localised}");
                        if (signal.Type_Localised.StartsWith("Geo", StringComparison.OrdinalIgnoreCase))
                            hasGeo = true;
                        if (signal.Type_Localised.StartsWith("Bio", StringComparison.OrdinalIgnoreCase))
                            hasBio = true;
                    }

                    int total = signals.Signals.Sum(s => s.Count);
                    string signalsText = total == 1 ? "signal" : "signals";

                    if (hasBio)
                        log.DetailSsml.AppendUnspoken(Emojis.BioSignals);
                    if (hasGeo)
                        log.DetailSsml.AppendUnspoken(Emojis.GeoSignals);

                    if (list.Count <= 2)
                        log.DetailSsml.Append($"Sensors found {String.Join(" and ", list)} {signalsText}.");
                    else
                        log.DetailSsml.Append($"Sensors found {String.Join(", ", list.Take(list.Count - 1))} and {list.Last()} {signalsText}.");
                }

                Bridge.Instance.LogEvent(log);
            }
        }
    }
}
