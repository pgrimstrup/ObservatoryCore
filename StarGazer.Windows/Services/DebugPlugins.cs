using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarGazer.Framework.Interfaces;

namespace StarGazer.UI.Services
{
    internal class DebugPlugins : IDebugPlugins
    {
        public IDictionary<string, string> PluginTypes => new Dictionary<string, string> {
            { "Bridge", "StarGazer.Bridge.Bridge, StarGazer.Bridge" },
            { "Explorer", "StarGazer.Explorer.ExplorerWorker, StarGazer.Explorer" },
            { "Herald", "StarGazer.Herald.HeraldNotifier, StarGazer.Herald" }
        };
    }
}
