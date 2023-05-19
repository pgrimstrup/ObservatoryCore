using Observatory.Framework.Files.Journal;
using StarGazer.Framework;

namespace StarGazer.Bridge.Events
{
    internal class FSSAllBodiesFoundEventHandler : BaseEventHandler, IJournalEventHandler<FSSAllBodiesFound>
    {
        public void HandleEvent(FSSAllBodiesFound journal)
        {
            if (GameState.ScanPercent == 100)
                return;

            CreateOrrery(out int starCount, out int planetCount, out Scan primaryStar);
            string stars = $"{starCount} {Stars(starCount)}";
            string andPlanets = "";
            if (planetCount > 0)
                andPlanets = $" and {planetCount} {Planets(planetCount)}";

            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Science Station");
            log.DetailSsml
                .Append($"System Scan Complete")
                .AppendEmphasis("Commander.", EmphasisType.Moderate);

            if (primaryStar.WasDiscovered)
                log.DetailSsml.Append($"We've discovered {stars}{andPlanets}.");
            else
                log.DetailSsml.Append($"We are the first to discover this system consisting of {stars}{andPlanets}.");

            log.Send();
            GameState.ScanPercent = 100;
        }
    }
}
