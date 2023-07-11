using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace StarGazer.Bridge
{
    public class JumpDestination
    {
        public ulong SystemAddress { get; set; }
        public string StarSystem { get; set; } = "";
        public string StarClass { get; set; } = "";
        public (double x, double y, double z) StarPos { get; set; }
        public int RemainingJumpsInRoute { get; set; }

        public void Set(ulong system, string name, string starClass,  (double x, double y, double z) starPos)
        {
            SystemAddress = system;
            StarSystem = name;
            StarClass = starClass;
            StarPos = starPos;
        }

        public void Clear()
        {
            SystemAddress = 0;
            StarSystem = "";
            StarClass = "";
            StarPos = (0, 0, 0);
        }
    }
}
