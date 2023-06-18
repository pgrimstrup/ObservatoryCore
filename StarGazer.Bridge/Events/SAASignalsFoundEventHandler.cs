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

            int totalCount = journal.Signals.Sum(s => s.Count);
            if (totalCount > 0)
            {
                var log = new BridgeLog(journal);
                log.SpokenOnly();
                log.TitleSsml.AppendBodyName(GetBodyName(journal.BodyName));
                BridgeUtils.AppendSignalInfo(journal.BodyName, journal.Signals, log);
                log.Send();

                var scanEntry = FindLogEntry(nameof(Scan), log.Title);
                UpdateSignals(scanEntry, journal.BodyName, journal.Signals);
            }
        }
    }
}
