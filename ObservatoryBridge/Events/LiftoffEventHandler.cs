using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class LiftoffEventHandler : BaseEventHandler, IJournalEventHandler<Liftoff>
    {
        public void HandleEvent(Liftoff journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.AppendUnspoken(Emojis.Liftoff);
            if (Bridge.Instance.CurrentShip.Status.HasFlag(Framework.Files.ParameterTypes.StatusFlags.SRV))
            {
                log.DetailSsml
                   .Append($"Ship is returning to orbit")
                   .AppendEmphasis("Commander", Framework.EmphasisType.Moderate);
            }
            if (Bridge.Instance.CurrentShip.Status.HasFlag(Framework.Files.ParameterTypes.StatusFlags.MainShip))
            {
                log.DetailSsml
                   .Append($"Liftoff complete from")
                    .AppendBodyName(GetBodyName(journal.Body));
            }

            Bridge.Instance.LogEvent(log);
        }
    }
}
