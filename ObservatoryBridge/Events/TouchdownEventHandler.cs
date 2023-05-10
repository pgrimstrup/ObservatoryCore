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
            log.DetailSsml
                .Append($"Touchdown on body")
                .AppendBodyName(GetBodyName(journal.Body))
                .Append("completed")
                .AppendEmphasis("Commander", EmphasisType.Moderate);

            Bridge.Instance.LogEvent(log);
        }
    }
}
