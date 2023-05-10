using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class FSDJumpEventHandler : BaseEventHandler, IJournalEventHandler<FSDJump>
    {
        public void HandleEvent(FSDJump journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");

            log.DetailSsml
                .Append($"Jump completed Commander. Arrived at")
                .AppendBodyName(journal.StarSystem)
                .Append(". We travelled")
                .AppendNumber(Math.Round(journal.JumpDist, 2))
                .Append("light years, using")
                .AppendNumber(Math.Round(journal.FuelUsed, 2))
            .Append("tons of fuel.");

            Bridge.Instance.Core.ExecuteOnUIThread(() => {
                // Remove all entries except for the last Start Jump entry
                var keep = Bridge.Instance.Events.Cast<BridgeLog>().LastOrDefault(e => e.Title.StartsWith("FSD Jump"));
                while (Bridge.Instance.Events.Count > 0)
                {
                    if (Bridge.Instance.Events[0] == keep)
                        break;
                    Bridge.Instance.Events.RemoveAt(0);
                }
            });

            Bridge.Instance.LogEvent(log);
            Bridge.Instance.CurrentSystem = new CurrentSystemData(journal);
        }
    }
}
