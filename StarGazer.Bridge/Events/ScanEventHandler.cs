using System.Numerics;
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

            // Confirm the current system 
            if (GameState.CurrentSystem.SystemAddress != journal.SystemAddress)
                GameState.CurrentSystem.Set(journal.SystemAddress, journal.StarSystem, "", (0, 0, 0));

            GameState.ScannedBodies.Add(journal.BodyName, journal);

            if (!String.IsNullOrEmpty(journal.StarType))
            {
                // Otherwise, stars are text-logged only and not spoken
                var log = new BridgeLog(journal);
                log.TextOnly();
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

                if (journal.DistanceFromArrivalLS < 1)
                {
                    // This can be missing as a result of a CarrierJump
                    GameState.CurrentSystem.StarClass = journal.StarType;
                    if (journal.WasDiscovered)
                        log.DetailSsml.Append("Previously discovered primary star.");
                    else
                        log.DetailSsml.Append("First discovery of primary star.");
                }

                if (journal.DistanceFromArrivalLS < 1)
                    log.Distance = "Primary Star";
                else
                    log.Distance = $"{journal.DistanceFromArrivalLS:n0} LS";

                if (journal.WasDiscovered)
                    log.Discovered = Emojis.AlreadyDiscovered + " Yes";
                else
                    log.Discovered = Emojis.FirstDiscovery + " First";

                log.Send();

                // If this is the primary star, and it is NotDiscovered, then alert CMDR that this is a first discovery system.
                if (journal.DistanceFromArrivalLS < 1 && !journal.WasDiscovered)
                {
                    log = new BridgeLog();
                    log.SpokenOnly();
                    log.DetailSsml
                        .AppendEmphasis("Commander,", EmphasisType.Moderate)
                        .Append("star charts indicate that we are the first to discover this system.");
                    log.Send();
                }
            }
            else if (!String.IsNullOrEmpty(journal.PlanetClass)) // ignore belt clusters
            {
                var k_value = BodyValueEstimator.GetKValueForBody(journal.PlanetClass, !String.IsNullOrEmpty(journal.TerraformState));
                var currentValue = BodyValueEstimator.GetBodyValue(k_value, journal.MassEM, !journal.WasDiscovered, false, !journal.WasMapped, false);
                var mappedValue = BodyValueEstimator.GetBodyValue(k_value, journal.MassEM, !journal.WasDiscovered, true, !journal.WasMapped, true);

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

                BridgeLog? spokenOnly = null;
                var log = new BridgeLog(journal);
                log.IsTitleSpoken = true;
                log.TitleSsml.AppendBodyName(GetBodyName(journal.BodyName));

                if (emojies.Count > 0)
                    log.DetailSsml.AppendUnspoken(String.Join("", emojies));

                if (journal.Landable)
                    log.DetailSsml.Append("Landable");

                if (!String.IsNullOrEmpty(journal.TerraformState))
                    log.DetailSsml.Append($"{journal.TerraformState}");

                log.DetailSsml
                    .AppendBodyType(journal.PlanetClass);

                if (!String.IsNullOrEmpty(journal.Atmosphere))
                    log.DetailSsml.Append($"with {journal.Atmosphere}.");
                else if (journal.Landable)
                    log.DetailSsml.Append("with no atmosphere."); // Only for landable bodies, let's make this explicit
                else
                    log.DetailSsml.Append("."); // Non-landable, so don't care

                if (GameState.BodySignals.TryGetValue(journal.BodyName, out var signals))
                {
                    UpdateSignals(log, journal.BodyName, signals.Signals);

                    spokenOnly ??= new BridgeLog(journal).SpokenOnly();
                    BridgeUtils.AppendSignalInfo(journal.BodyName, signals.Signals, spokenOnly);
                }

                //if (mappedValue >= Bridge.Instance.Settings.HighValueBody)
                //    log.Discovered = Emojis.HighValue;

                if (journal.WasDiscovered)
                {
                    if (mappedValue >= Bridge.Instance.Settings.HighValueBody)
                        log.Discovered = Emojis.HighValueAlreadyDiscovered + " High Value";
                    else
                        log.Discovered = Emojis.AlreadyDiscovered + " Yes";
                }
                else
                {
                    if (mappedValue >= Bridge.Instance.Settings.HighValueBody)
                        log.Discovered = Emojis.FirstDiscovery + " High Value";
                    else
                        log.Discovered = Emojis.FirstDiscovery + " First";
                }

                if (journal.WasMapped)
                    log.Mapped = Emojis.AlreadyMapped + " Yes";
                else
                    log.Mapped = Emojis.Unmapped + " No";

                log.CurrentValue = $"{currentValue:n0} Cr";
                log.MappedValue = $"{mappedValue:n0} Cr";
                log.Distance = $"{journal.DistanceFromArrivalLS:n0} LS";
                log.Send();

                if (mappedValue >= Bridge.Instance.Settings.HighValueBody)
                {
                    spokenOnly ??= new BridgeLog(journal).SpokenOnly();
                    spokenOnly.DetailSsml.Append($"Estimated value");
                    spokenOnly.DetailSsml.AppendNumber(mappedValue);
                    spokenOnly.DetailSsml.Append("credits.");
                }

                spokenOnly?.Send();
            }

            if (GameState.AutoCompleteScanCount > 0 && GameState.ScannedBodies.Count == GameState.AutoCompleteScanCount)
            {
                SendScanComplete(journal);
                GameState.ScanPercent = 100;
            }
        }
    }
}
