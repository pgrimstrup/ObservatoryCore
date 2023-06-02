using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class SupercruiseExitEventHandler : BaseEventHandler, IJournalEventHandler<SupercruiseExit>
    {
        public void HandleEvent(SupercruiseExit journal)
        {
            var log = new BridgeLog(journal);
            log.SpokenOnly();
            log.TitleSsml.Append("Flight Operations");
            log.DetailSsml.Append($"Exiting supercruise, sub-light engines active.");

            log.Send();
        }
    }
}
