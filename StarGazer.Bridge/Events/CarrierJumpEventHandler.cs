using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class CarrierJumpEventHandler : BaseEventHandler, IJournalEventHandler<CarrierJump>
    {
        public void HandleEvent(CarrierJump journal)
        {
            TryGetStationName(journal.StationName, out string carrierName);

            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Fleet Carrier");

            log.DetailSsml
                .AppendEmphasis("Commander,", Framework.EmphasisType.Moderate)
                .Append($"Fleet Carrier")
                .AppendEmphasis(carrierName, Framework.EmphasisType.Moderate)
                .Append("has completed its jump.")
                .Append("We have arrived at")
                .AppendBodyName(journal.StarSystem)
                .EndSentence();

            Bridge.Instance.ResetLogEntries();

            log.Send();

            // Next time we prepare or start a jump, we need to speak the destination
            GameState.DestinationTimeToSpeak = DateTime.Now;
            GameState.RemainingJumpsInRouteTimeToSpeak = DateTime.Now;
        }
    }
}
