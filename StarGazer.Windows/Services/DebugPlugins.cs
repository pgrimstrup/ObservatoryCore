using StarGazer.Framework.Interfaces;

namespace StarGazer.UI.Services
{
    internal class DebugPlugins : IDebugPlugins
    {
        public IDictionary<string, string> PluginTypes => new Dictionary<string, string> {
            { "Bridge", "StarGazer.Bridge.Bridge, StarGazer.Bridge" },
            { "Explorer", "StarGazer.Explorer.ExplorerWorker, StarGazer.Explorer" },
            { "Herald", "StarGazer.Herald.HeraldNotifier, StarGazer.Herald" },
            { "EDSM", "StarGazer.EDSM.EdsmWorker, StarGazer.EDSM" }
        };
    }
}
