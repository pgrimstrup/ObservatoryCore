using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class ApproachBodyEventHandler : BaseEventHandler, IJournalEventHandler<ApproachBody>
    {
        public void HandleEvent(ApproachBody journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");

            log.DetailSsml.AppendUnspoken(Emojis.Approaching);
            log.DetailSsml.Append($"On approach to")
                .AppendBodyName(GetBodyName(journal.Body))
                .EndSentence();

            if(GameState.ScannedBodies.TryGetValue(journal.Body, out var scan))
            {
                double g = scan.SurfaceGravity / 9.803;
                if(scan.SurfaceGravity < 1.5)
                {
                    log.DetailSsml.AppendEmphasis("Caution,", Framework.EmphasisType.Strong);
                    log.DetailSsml.Append($"This world has a very low")
                        .Append($"surface gravity of {Math.Round(g, 2)} G.");
                }
                else if (scan.SurfaceGravity >= 12)
                {
                    log.DetailSsml.AppendEmphasis("Caution,", Framework.EmphasisType.Strong);
                    log.DetailSsml.Append($"This is a high gravity world")
                        .Append($"with surface gravity of {Math.Round(g, 2)} G.");
                }
                else if (scan.SurfaceGravity < 9)
                {
                    log.DetailSsml.Append($"Surface gravity is low,")
                        .Append($"at {Math.Round(g, 2)} G.");
                }
                else
                {
                    log.DetailSsml.Append($"Standard surface gravity")
                        .Append($"at {Math.Round(g, 2)} G.");
                }
            }

            Bridge.Instance.LogEvent(log);
        }
    }
}
