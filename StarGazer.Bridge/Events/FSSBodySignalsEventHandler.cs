using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class FSSBodySignalsEventHandler : BaseEventHandler, IJournalEventHandler<FSSBodySignals>
    {
        public void HandleEvent(FSSBodySignals journal)
        {
            GameState.BodySignals[journal.BodyName] = journal;
        }
    }
}
