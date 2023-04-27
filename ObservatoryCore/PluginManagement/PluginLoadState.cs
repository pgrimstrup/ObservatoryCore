using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Interfaces;

namespace Observatory.PluginManagement
{
    public class PluginLoadState
    {
        public Exception Error { get; set; }

        public string SettingKey { get; set; }
        public string TypeName { get; set; }
        public IObservatoryPlugin Instance { get; set; }
    }
}
