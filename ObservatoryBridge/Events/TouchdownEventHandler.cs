using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class TouchdownEventHandler : BaseEventHandler, IJournalEventHandler<Touchdown>
    {
        public void HandleEvent(Touchdown journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.AppendUnspoken(Emojis.Touchdown);
            if (Bridge.Instance.CurrentShip.Status.HasFlag(Framework.Files.ParameterTypes.StatusFlags.SRV))
            {
                log.DetailSsml
                    .Append("Ship has returned from orbit and is ready to board")
                    .AppendEmphasis("Commander", EmphasisType.Moderate);
            }
            if (Bridge.Instance.CurrentShip.Status.HasFlag(Framework.Files.ParameterTypes.StatusFlags.MainShip))
            {
                if (String.IsNullOrWhiteSpace(journal.Body))
                {
                    log.DetailSsml.Append("Touchdown").AppendEmphasis("Commander", EmphasisType.Moderate);
                }
                else
                {
                    log.DetailSsml
                        .Append($"Touchdown on")
                        .AppendBodyName(GetBodyName(journal.Body))
                        .Append("completed")
                        .AppendEmphasis("Commander", EmphasisType.Moderate);
                }
            }
            Bridge.Instance.LogEvent(log);
        }
    }
}
