using Observatory.Framework.Files.Journal;
using Observatory.Framework.Files.ParameterTypes;
using StarGazer.Framework;

namespace StarGazer.Bridge.Events
{
    internal class FSDTargetEventHandler : BaseEventHandler, IJournalEventHandler<FSDTarget>
    {
        public void HandleEvent(FSDTarget journal)
        {
            GameState.Assign(journal);

            // This event is received mid-jump when auto-plotting to the next star system in the route.
            // It also occurs when the ship is returning from orbit while on the surface.
            if (GameState.Status.HasFlag(StatusFlags.FSDJump) || !GameState.Status.HasFlag(StatusFlags.MainShip))
                return;

            // Manually selected the next destination system
            if (!String.IsNullOrEmpty(journal.Name))
            {
                if (GameState.NextDestinationTimeToSpeak > DateTime.Now || journal.Name != GameState.NextSystemName)
                    return;

                var log = new BridgeLog(journal);
                log.TitleSsml.Append("Flight Operations");

                var fuelStar = journal.StarClass.IsFuelStar() ? ", a fuel star" : "";
                log.DetailSsml
                    .Append("Course laid in to")
                        .AppendBodyName(journal.Name)
                        .Append($". Destination star is a")
                        .AppendBodyType(GetStarTypeName(journal.StarClass))
                        .Append($"{fuelStar}.");

                if (GameState.RemainingJumpsInRoute == 1)
                    log.DetailSsml.Append($"This is the final jump in the current flight plan.");
                else if (GameState.RemainingJumpsInRoute > 1 && GameState.RemainingJumpsInRouteTimeToSpeak < DateTime.Now && (GameState.RemainingJumpsInRoute < 5 || (GameState.RemainingJumpsInRoute % 5) == 0))
                {
                    log.DetailSsml.Append($"There are {GameState.RemainingJumpsInRoute} jumps remaining in the current flight plan.");
                    GameState.RemainingJumpsInRouteTimeToSpeak = DateTime.Now.Add(SpokenDestinationInterval * 2);
                }

                if (journal.StarClass.IsNeutronStar() || journal.StarClass.IsWhiteDwarf() || journal.StarClass.IsBlackHole())
                {
                    log.DetailSsml
                        .AppendEmphasis("Commander,", EmphasisType.Moderate)
                        .Append("that is a hazardous star type.")
                        .AppendEmphasis("Caution is advised", EmphasisType.Moderate)
                        .Append("on exiting jump");
                }

                log.Send();
                if(!Bridge.Instance.Core.IsLogMonitorBatchReading)
                    GameState.NextDestinationTimeToSpeak = DateTime.Now.Add(SpokenDestinationInterval);
            }
        }
    }
}
