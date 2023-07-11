using Observatory.Framework.Files.Journal;
using Observatory.Framework.Files.ParameterTypes;
using StarGazer.Framework;

namespace StarGazer.Bridge.Events
{
    internal class FSDTargetEventHandler : BaseEventHandler, IJournalEventHandler<FSDTarget>
    {
        public void HandleEvent(FSDTarget journal)
        {
            // This event is received mid-jump when auto-plotting to the next star system in the route.
            // It also occurs when the ship is returning from orbit while on the surface (ie, Cmdr not in MainShip)
            if (GameState.Status.HasFlag(StatusFlags.FSDJump) || !GameState.Status.HasFlag(StatusFlags.MainShip))
                return;

            // Manually selected the next destination system
            if (!String.IsNullOrEmpty(journal.Name))
            {
                if (GameState.DestinationTimeToSpeak >= DateTime.Now)
                    return;

                var log = new BridgeLog(journal);
                log.TitleSsml.Append("Flight Operations");

                var fuelStar = journal.StarClass.IsFuelStar() ? ", a fuel star" : "";
                log.DetailSsml
                    .Append("Jump course laid in to")
                        .AppendBodyName(journal.Name)
                        .Append($". Destination star is a")
                        .AppendBodyType(GetStarTypeName(journal.StarClass))
                        .Append($"{fuelStar}.");

                AppendRemainingJumps(log, true);
                AppendHazardousStarWarning(log, journal.StarClass);

                log.Send();
                GameState.DestinationTimeToSpeak = DateTime.Now.Add(SpokenDestinationInterval);
            }
        }
    }
}
