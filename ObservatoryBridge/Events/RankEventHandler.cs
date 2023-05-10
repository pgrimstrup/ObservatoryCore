using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class RankEventHandler : BaseEventHandler, IJournalEventHandler<Rank>
    {
        public void HandleEvent(Rank journal)
        {
            // Record current rank so we can detect promotions
            Bridge.Instance.CurrentRank = journal;
        }
    }
}
