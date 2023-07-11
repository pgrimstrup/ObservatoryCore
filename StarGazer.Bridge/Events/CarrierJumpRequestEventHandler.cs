using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class CarrierJumpRequestEventHandler : BaseEventHandler, IJournalEventHandler<CarrierJumpRequest>
    {
        public void HandleEvent(CarrierJumpRequest journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Fleet Carrier");

            TryGetStationName(GameState.CurrentLocation.Name, out string carrierName);

            TimeSpan when = DateTime.Parse(journal.DepartureTime).Subtract(journal.TimestampDateTime);
            string remaining;
            if (when.TotalMinutes < 2)
                remaining = "second".CountAndPlural((int)when.TotalSeconds);
            else
                remaining = "minute".CountAndPlural((int)when.TotalMinutes);

            log.DetailSsml
                .AppendEmphasis("Commander,", Framework.EmphasisType.Moderate)
                .Append("Fleet Carrier")
                .AppendEmphasis(carrierName, Framework.EmphasisType.Moderate)
                .Append("will be jumping to")
                .AppendBodyName(journal.SystemName)
                .Append($"in {remaining}.");

            log.Send();
        }
    }
}
