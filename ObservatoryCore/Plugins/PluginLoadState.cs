using Observatory.Framework.Interfaces;

namespace Observatory.Plugins
{
    public class PluginLoadState
    {
        public Exception Error { get; set; }

        public string SettingKey { get; set; }
        public string TypeName { get; set; }
        public IObservatoryPlugin Instance { get; set; }
    }
}
