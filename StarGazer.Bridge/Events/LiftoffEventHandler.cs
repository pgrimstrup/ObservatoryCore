using Observatory.Framework.Files.Journal;
using Observatory.Framework.Files.ParameterTypes;

namespace StarGazer.Bridge.Events
{
    internal class LiftoffEventHandler : BaseEventHandler, IJournalEventHandler<Liftoff>
    {
        public void HandleEvent(Liftoff journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.AppendUnspoken(Emojis.Liftoff);
            if (GameState.Status.HasFlag(StatusFlags.SRV))
            {
                log.DetailSsml
                   .Append($"Ship is returning to orbit")
                   .AppendEmphasis("Commander", Framework.EmphasisType.Moderate);
            }
            if (GameState.Status.HasFlag(StatusFlags.MainShip))
            {
                log.DetailSsml
                   .Append($"Liftoff complete from")
                    .AppendBodyName(GetBodyName(journal.Body));
            }

            Bridge.Instance.LogEvent(log);
        }
    }
}
