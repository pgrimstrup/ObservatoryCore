using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge
{
    internal class CurrentSystemData
    {
        readonly FSDJump _jump;

        public string? SystemName => _jump.StarSystem;

        public bool ScanComplete { get; set; }

        public Dictionary<string, Scan> ScannedBodies { get; } = new Dictionary<string, Scan>();

        public Dictionary<string, FSSBodySignals> BodySignals { get; } = new Dictionary<string, FSSBodySignals>();

        public CurrentSystemData(FSDJump journal) 
        {
            _jump = journal;
        }
    }
}
