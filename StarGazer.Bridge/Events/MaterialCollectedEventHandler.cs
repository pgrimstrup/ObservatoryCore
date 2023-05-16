using Observatory.Framework.Files.Journal;

namespace StarGazer.Bridge.Events
{
    internal class MaterialCollectedEventHandler : BaseEventHandler, IJournalEventHandler<MaterialCollected>
    {
        public void HandleEvent(MaterialCollected journal)
        {
            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Away Team");
            log.DetailSsml.Append($"Collected {journal.Count} units of")
                .AppendEmphasis(journal.Name_Localised ?? journal.Name, Framework.EmphasisType.Moderate);

            Bridge.Instance.LogEvent(log);
        }
    }
}
