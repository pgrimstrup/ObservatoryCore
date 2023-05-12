using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge.Events
{
    internal class MusicEventHandler : BaseEventHandler, IJournalEventHandler<Music>
    {
        public void HandleEvent(Music journal)
        {
            LogInfo($"Music: {journal.MusicTrack}");
        }
    }
}
