using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal interface IJournalEventHandler<T> where T : JournalBase
    {
        void HandleEvent(T journal);
    }
}
