using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files;
using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class NavRouteFileEventHandler : BaseEventHandler, IJournalEventHandler<NavRouteFile>
    {
        public void HandleEvent(NavRouteFile journal)
        {
            if (journal.Route == null || journal.Route.Count == 0)
            {
                GameState.RouteDestination.Clear();
                return;
            }

            // We won't track the entire route at this point
            var current = journal.Route.First();
            var next = journal.Route.Skip(1).First();
            var destination = journal.Route.Last();
            GameState.RouteDestination.Set(destination.SystemAddress, destination.StarSystem, destination.StarClass, destination.StarPos);
            GameState.RouteDestination.RemainingJumpsInRoute = journal.Route.Count - 1;

            GameState.CurrentSystem.Set(current.SystemAddress, current.StarSystem, current.StarClass, current.StarPos);
            GameState.JumpDestination.Set(next.SystemAddress, next.StarSystem, next.StarClass, next.StarPos);

            // If there is only one jump, then let FSDTarget event handle the notification
            if (journal.Route.Count > 2)
            {

                var log = new BridgeLog(journal);
                log.SpokenOnly();

                log.DetailSsml
                    .Append("Flight plan established to")
                    .AppendBodyName(journal.Route.Last().StarSystem)
                    .Append(".");

                // Routed course includes current system, so there is one less jump
                log.DetailSsml.Append($"There are {journal.Route.Count - 1} jumps in the plotted course.");
                GameState.RemainingJumpsInRouteTimeToSpeak = DateTime.Now.Add(SpokenDestinationInterval * 2);

                double totalDistance = 0;
                for (int i = 1; i < journal.Route.Count; i++)
                    totalDistance += DistanceBetween(journal.Route[i - 1].StarPos, journal.Route[i].StarPos);

                log.DetailSsml.Append($"Total travel distance is {totalDistance:n2} light years.");
                log.Send();
            }
        }
    }
}
