using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class NavRouteEventHandler : BaseEventHandler, IJournalEventHandler<NavRoute>
    {
        public void HandleEvent(NavRoute journal)
        {
            LogInfo("NavRoute: NavRoute event occurred");
        }
    }
}
