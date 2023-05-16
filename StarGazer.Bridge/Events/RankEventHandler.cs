using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class RankEventHandler : BaseEventHandler, IJournalEventHandler<Rank>
    {
        public void HandleEvent(Rank journal)
        {
            // Record current rank so we can detect promotions
            Bridge.Instance.CurrentRank = journal;
        }
    }
}
