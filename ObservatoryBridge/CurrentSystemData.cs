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
        public int ScanPercent { get; set; }

        public string NextSystemName { get; set; } = "";
        public string NextStarClass { get; set; } = "";
        public int RemainingJumpsInRoute { get; set; }
        public DateTime NextDestinationNotify { get; set; }
        public bool FirstDiscoverySpoken { get; set; }

        public Dictionary<string, Scan> ScannedBodies { get; } = new Dictionary<string, Scan>();

        public Dictionary<string, FSSBodySignals> BodySignals { get; } = new Dictionary<string, FSSBodySignals>();

        public void Assign(FSDTarget target)
        {
            RemainingJumpsInRoute = target.RemainingJumpsInRoute;
            NextSystemName = target.Name;
            NextStarClass = target.StarClass;
            NextDestinationNotify = DateTime.MinValue; // immediately when needed
        }

        public void Assign(FSDJump jump)
        {
            FirstDiscoverySpoken = false;
            SystemName = jump.StarSystem;
            ScanPercent = 0;
            ScannedBodies.Clear();
            BodySignals.Clear();
        }
    }
}
