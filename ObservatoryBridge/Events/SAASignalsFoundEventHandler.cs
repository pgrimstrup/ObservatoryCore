using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class SAASignalsFoundEventHandler : BaseEventHandler, IJournalEventHandler<SAASignalsFound>
    {
        public void HandleEvent(SAASignalsFound journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.AppendBodyName(GetBodyName(journal.BodyName));

            List<string> signals = new List<string>();
            bool hasGeo = false;
            bool hasBio = false;
            foreach (var signal in journal.Signals)
            {
                var typename = signal.Type_Localised ?? signal.Type;
                signals.Add($"{signal.Count} {signal.Type_Localised}");
                if (typename.StartsWith("Geo", StringComparison.OrdinalIgnoreCase))
                    hasGeo = true;
                if (typename.StartsWith("Bio", StringComparison.OrdinalIgnoreCase))
                    hasBio = true;
            }

            var total = journal.Signals.Sum(s => s.Count);
            var plural = total == 1 ? "signal" : "signals";

            if (hasBio)
                log.DetailSsml.AppendUnspoken(Emojis.BioSignals);
            if (hasGeo)
                log.DetailSsml.AppendUnspoken(Emojis.GeoSignals);

            if (signals.Count <= 2)
                log.DetailSsml
                    .Append($"Sensors are picking up {String.Join(" and ", signals)} {plural} on")
                    .AppendBodyName(GetBodyName(journal.BodyName));
            else
                log.DetailSsml
                    .Append($"Sensors are picking up {String.Join(", ", signals.Take(signals.Count - 1))} and {signals.Last()} {plural} on")
                    .AppendBodyName(GetBodyName(journal.BodyName));

            Bridge.Instance.LogEvent(log);
        }
    }
}
