using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class NavRouteEventHandler : BaseEventHandler, IJournalEventHandler<NavRoute>
    {
        public void HandleEvent(NavRoute journal)
        {
        }
    }
}
