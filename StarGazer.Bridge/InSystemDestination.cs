using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarGazer.Bridge
{
    public class InSystemDestination
    {
        public ulong SystemAddress { get; set; }
        public int Body { get; set; }
        public string Name { get; set; } = "";

        public bool IsSet => SystemAddress > 0 || Body > 0 || !String.IsNullOrEmpty(Name);

        public void Set(ulong system, int body, string bodyName)
        {
            SystemAddress = system;
            Body = body;
            Name = bodyName;
        }

        public void Set(ulong system, string stationName)
        {
            SystemAddress = system;
            Body = 0;
            Name = stationName;
        }

        public void Clear()
        {
            SystemAddress = 0;
            Body = 0;
            Name = "";
        }
    }
}
