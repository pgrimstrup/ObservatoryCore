using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class ScanOrganicEventHandler : BaseEventHandler, IJournalEventHandler<ScanOrganic>
    {
        public void HandleEvent(ScanOrganic journal)
        {
            LogInfo($"{journal.Event}: {journal.Genus_Localised} - {journal.Species_Localised}");
        }
    }
}
