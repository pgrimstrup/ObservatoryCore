using Observatory.Framework.Files.Journal;
using Observatory.Framework.Files.ParameterTypes;

namespace StarGazer.Bridge.Events
{
    internal class SAASignalsFoundEventHandler : BaseEventHandler, IJournalEventHandler<SAASignalsFound>
    {
        public void HandleEvent(SAASignalsFound journal)
        {
            // Can also get this event when the ship is returning to the surface by itself
            if (!GameState.Status.HasFlag(StatusFlags.MainShip))
                return;

            if (GameState.BodySignals.TryGetValue(journal.BodyName, out var signals))
            {
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

                if (bioCount > 0 || geoCount > 0)
                {
                    var log = new BridgeLog(journal);
                    log.TitleSsml.AppendBodyName(GetBodyName(journal.BodyName));

                    log.DetailSsml.Append("Sensors are picking up");
                    if (bioCount > 0)
                        log.DetailSsml.Append($"{bioCount} biological");
                    if (bioCount > 0 && geoCount > 0)
                        log.DetailSsml.Append("and");
                    if (geoCount > 0)
                        log.DetailSsml.Append($"{geoCount} geological");
                    log.DetailSsml.Append("signal".Plural(bioCount + geoCount));
                    log.DetailSsml.EndSentence();

                    Bridge.Instance.LogEvent(log);
                }
            }
        }
    }
}
