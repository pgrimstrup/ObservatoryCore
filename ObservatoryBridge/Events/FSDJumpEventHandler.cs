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

            if (!Bridge.Instance.Core.IsLogMonitorBatchReading)
            {
                Bridge.Instance.Core.ExecuteOnUIThread(() => {
                    // Remove all entries up to the last FSD Jump
                    var lastJump = Bridge.Instance.Events
                        .OfType<BridgeLog>()
                        .LastOrDefault(e => e.EventName == nameof(FSDJump));

                    if(lastJump != null)
                    {
                        var keepIndex = Bridge.Instance.Events.IndexOf(lastJump);
                        while(keepIndex > 0 && Bridge.Instance.Events.Count > 0)
                        {
                            Bridge.Instance.Events.RemoveAt(0);
                            keepIndex--;
                        }
                    }
                });
            }

            Bridge.Instance.LogEvent(log);
            Bridge.Instance.CurrentSystem = new CurrentSystemData(journal);
        }
    }
}
