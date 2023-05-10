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
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Fuel Scooping");

            log.DetailSsml.AppendUnspoken(Emojis.FuelScoop);
            log.DetailSsml
                .Append($"Scooped")
                .AppendNumber(Math.Round(journal.Scooped, 2))
                .Append("tons.");
            
            float total = (float)Math.Round(journal.Total, 2);
            if (total >= Bridge.Instance.CurrentShip.FuelCapacity)
                log.DetailSsml.Append("Main Tank full");
            else
                log.DetailSsml.Append("Main Fuel at")
                    .AppendNumber(Math.Round(journal.Total, 1))
                    .Append("tons");


            Bridge.Instance.LogEvent(log);
        }
    }
}
