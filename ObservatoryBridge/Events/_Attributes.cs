using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal interface IJournalEventHandler<T> where T : JournalBase
    {
        void HandleEvent(T journal);
    }
}
