using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Interfaces;

namespace ObservatoryUI.WPF.Services
{
    internal class DebugPlugins : IDebugPlugins
    {
        public IDictionary<string, string> PluginTypes => new Dictionary<string, string> {
            { "Botanist", "Observatory.Botanist.Botanist, ObservatoryBotanist" },
            { "Bridge", "Observatory.Bridge.Bridge, ObservatoryBridge" },
            { "Explorer", "Observatory.Explorer.ExplorerWorker, ObservatoryExplorer" },
            { "Herald", "Observatory.Herald.HeraldNotifier, ObservatoryHerald" }
        };
    }
}
