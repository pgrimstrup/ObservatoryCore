using Observatory.Framework.Files.Journal;
using Observatory.Framework.Files.ParameterTypes;

namespace StarGazer.Bridge.Events
{
    internal class SAASignalsFoundEventHandler : BaseEventHandler, IJournalEventHandler<SAASignalsFound>
    {
        public void HandleEvent(SAASignalsFound journal)
        {
            // Can also get this event when the ship is returning to the surface by itself
            if (!GameState.Status.HasFlag(StatusFlags.MainShip))
                return;

            if (GameState.BodySignals.TryGetValue(journal.BodyName, out var signals))
            {
                if(signals.Signals.Sum(s => s.Count) > 0)
                {
                    var log = new BridgeLog(journal);
                    log.TitleSsml.AppendBodyName(GetBodyName(journal.BodyName));

                    BridgeUtils.AppendSignalInfo(journal.BodyName, log);
                    Bridge.Instance.LogEvent(log);
                }
            }
        }
    }
}
