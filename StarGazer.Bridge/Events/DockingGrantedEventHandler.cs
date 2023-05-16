using Observatory.Framework.Files.Journal;
using StarGazer.Framework;

namespace StarGazer.Bridge.Events
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
