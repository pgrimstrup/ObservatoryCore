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
            log.DetailSsml
                .Append($"Liftoff complete from body")
                .AppendBodyName(GetBodyName(journal.Body));

            Bridge.Instance.LogEvent(log);
        }
    }
}
