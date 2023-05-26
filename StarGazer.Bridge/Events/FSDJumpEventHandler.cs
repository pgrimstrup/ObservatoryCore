using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class FSDJumpEventHandler : BaseEventHandler, IJournalEventHandler<FSDJump>
    {
        public void HandleEvent(FSDJump journal)
        {
            GameState.Assign(journal);

            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Flight Operations");

            string arrivedAt = "Arrived at";
            if (String.IsNullOrEmpty(GameState.NextSystemName) && !Bridge.Instance.Core.IsLogMonitorBatchReading)
                arrivedAt = "We have reached our destination, system";

            log.DetailSsml
                .Append($"Jump completed")
                .AppendEmphasis("Commander.", Framework.EmphasisType.Moderate)
                .Append(arrivedAt)
                .AppendBodyName(journal.StarSystem)
                .Append(". We travelled")
                .AppendNumber(Math.Round(journal.JumpDist, 2))
                .Append("light years, using")
                .AppendNumber(Math.Round(journal.FuelUsed, 2))
            .Append("tons of fuel.");

            if (!Bridge.Instance.Core.IsLogMonitorBatchReading)
            {
                Bridge.Instance.Core.ExecuteOnUIThread(() => {
                    // Remove all entries up to the last Start Jump
                    var lastJump = Bridge.Instance.PluginUI.DataGrid
                        .OfType<BridgeLog>()
                        .LastOrDefault(e => e.EventName == "StartJump");

                    if (lastJump != null)
                    {
                        var keepIndex = Bridge.Instance.PluginUI.DataGrid.IndexOf(lastJump);
                        while (keepIndex > 0 && Bridge.Instance.PluginUI.DataGrid.Count > 0)
                        {
                            Bridge.Instance.PluginUI.DataGrid.RemoveAt(0);
                            keepIndex--;
                        }
                    }
                });
            }

            log.Send();
        }
    }
}
