using Observatory.Framework.Files.Journal;
using Observatory.Framework.Files.ParameterTypes;
using StarGazer.Framework;

namespace StarGazer.Bridge.Events
{
    internal class TouchdownEventHandler : BaseEventHandler, IJournalEventHandler<Touchdown>
    {
        public void HandleEvent(Touchdown journal)
        {
            var log = new BridgeLog(journal);
            log.SpokenOnly();
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.AppendUnspoken(Emojis.Touchdown);
            if (GameState.Status.HasFlag(StatusFlags.SRV))
            {
                log.DetailSsml
                    .Append("Ship has returned from orbit and is ready to board")
                    .AppendEmphasis("Commander", EmphasisType.Moderate);
            }
            if (GameState.Status.HasFlag(StatusFlags.MainShip))
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
