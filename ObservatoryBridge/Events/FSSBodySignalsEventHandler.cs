using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class FSSBodySignalsEventHandler : BaseEventHandler, IJournalEventHandler<FSSBodySignals>
    {
        public void HandleEvent(FSSBodySignals journal)
        {
            Bridge.Instance.CurrentSystem.BodySignals[journal.BodyName] = journal;
        }
    }
}
