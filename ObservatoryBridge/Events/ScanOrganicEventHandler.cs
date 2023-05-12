using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class ScanOrganicEventHandler : BaseEventHandler, IJournalEventHandler<ScanOrganic>
    {
        public void HandleEvent(ScanOrganic journal)
        {
            LogInfo($"{journal.Event}: {journal.Genus_Localised} - {journal.Species_Localised}");
        }
    }
}
