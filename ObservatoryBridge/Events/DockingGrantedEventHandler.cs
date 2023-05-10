using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class DockingGrantedEventHandler : BaseEventHandler, IJournalEventHandler<DockingGranted>
    {
        public void HandleEvent(DockingGranted journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");

            log.DetailSsml
                .Append($"{journal.StationName} Tower has granted our docking request")
                .AppendEmphasis("Commander.", EmphasisType.Moderate)
                .Append("Heading to landing pad")
                .AppendEmphasis(journal.LandingPad.ToString(), EmphasisType.Moderate).EndSentence();

            Bridge.Instance.LogEvent(log);
        }
    }
}
