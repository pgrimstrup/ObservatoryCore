using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class FuelScoopEventHandler : BaseEventHandler, IJournalEventHandler<FuelScoop>
    {
        public void HandleEvent(FuelScoop journal)
        {
            LogInfo($"{journal.Event}: Scooped {journal.Scooped:n2} tons, {journal.Total:n2}/{Bridge.Instance.CurrentShip.FuelCapacity}");

            // Accumulate fuel scooping until we get a status change
            Bridge.Instance.FuelScooped += journal.Scooped;
            Bridge.Instance.FuelTotal = journal.Total;

            // Fuel Scooping Completed is slightly different to Fuel Scooping terminated.
            double total = Math.Round(journal.Total, 2);
            if (total >= Bridge.Instance.CurrentShip.FuelCapacity)
            {
                var log = new BridgeLog(journal);
                log.TitleSsml.Append("Fuel Scooping");

                log.DetailSsml.AppendUnspoken(Emojis.FuelScoop);
                log.DetailSsml
                    .Append($"Fuel scooping completed, collected")
                    .AppendNumber(Math.Round(Bridge.Instance.FuelScooped, 2))
                    .Append("tons.");

                log.DetailSsml.Append("Main tank full.");

                Bridge.Instance.LogEvent(log);
                Bridge.Instance.FuelScooped = 0;
                Bridge.Instance.FuelTotal = 0;
            }
        }
    }
}
