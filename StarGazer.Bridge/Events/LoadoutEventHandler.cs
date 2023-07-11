using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class LoadoutEventHandler : BaseEventHandler, IJournalEventHandler<Loadout>
    {
        public void HandleEvent(Loadout journal)
        {
        }
    }
}
