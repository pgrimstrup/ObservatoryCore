using Observatory.Framework.Interfaces;
using StarGazer.Framework.Interfaces;

namespace StarGazer.Plugins
{
    public class PluginLoadState
    {
        public Exception Error { get; set; }

        public string SettingKey { get; set; }
        public string TypeName { get; set; }
        public IObservatoryPlugin Instance { get; set; }
    }
}
