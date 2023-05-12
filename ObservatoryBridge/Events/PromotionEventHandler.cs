using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;
using Observatory.Framework.Files.ParameterTypes;

namespace Observatory.Bridge.Events
{
    internal class PromotionEventHandler : BaseEventHandler, IJournalEventHandler<Promotion>
    {
        public void HandleEvent(Promotion journal)
        {
            if (Bridge.Instance.CurrentRank == null)
            {
                Bridge.Instance.CurrentRank = journal;
                return;
            }

            List<string> promotions = new List<string>();
            if (journal.Empire > RankEmpire.None && journal.Empire > Bridge.Instance.CurrentRank.Empire)
                promotions.Add($"Empire rank {journal.Empire}");
            if (journal.Federation > RankFederation.None && journal.Federation > Bridge.Instance.CurrentRank.Federation)
                promotions.Add($"Federation rank {journal.Federation}");

            if (journal.Trade > Bridge.Instance.CurrentRank.Trade)
                promotions.Add($"Trade rank {journal.Trade}");
            if (journal.Explore > Bridge.Instance.CurrentRank.Explore)
                promotions.Add($"Exploration rank {journal.Explore}");
            if (journal.Exobiologist > Bridge.Instance.CurrentRank.Exobiologist)
                promotions.Add($"Exobiologist rank {journal.Exobiologist}");
            if (journal.Combat > Bridge.Instance.CurrentRank.Combat)
                promotions.Add($"Combat rank {journal.Combat}");
            if (journal.CQC > Bridge.Instance.CurrentRank.CQC)
                promotions.Add($"CQC rank {journal.CQC}");
            if (journal.Soldier > Bridge.Instance.CurrentRank.Soldier)
                promotions.Add($"Soldier rank {journal.Soldier}");

            Bridge.Instance.CurrentRank = journal;
            if (promotions.Count == 0)
                return;

            var log = new BridgeLog(journal);
            log.TitleSsml.Append("Promotion");

            log.DetailSsml.AppendUnspoken(Emojis.Promotion);
            log.DetailSsml.Append("Congratulations")
                .AppendEmphasis("Commander.", Framework.EmphasisType.Moderate)
                .Append("You have been promoted to");

            if (promotions.Count <= 2)
                log.DetailSsml.Append(String.Join(" and ", promotions) + ".");
            else
                log.DetailSsml.Append(String.Join(", ", promotions.Take(promotions.Count - 1)) + " and " + promotions.Last() + ".");

            Bridge.Instance.LogEvent(log);
        }
    }
}
