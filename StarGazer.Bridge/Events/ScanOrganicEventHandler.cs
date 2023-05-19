using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class ScanOrganicEventHandler : BaseEventHandler, IJournalEventHandler<ScanOrganic>
    {
        public void HandleEvent(ScanOrganic journal)
        {
        }
    }
}
