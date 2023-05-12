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
        public string SystemName { get; set; } = "Unknown";
        public string CoursePlotted { get; set; } = "";
        public bool ScanComplete { get; set; }

        public Dictionary<string, Scan> ScannedBodies { get; } = new Dictionary<string, Scan>();

        public Dictionary<string, FSSBodySignals> BodySignals { get; } = new Dictionary<string, FSSBodySignals>();

        public void Assign(FSDJump jump)
        {
            SystemName = jump.StarSystem;
            CoursePlotted = "";
            ScanComplete = false;
            ScannedBodies.Clear();
            BodySignals.Clear();
        }
    }
}
