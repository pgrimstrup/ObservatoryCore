using Observatory.Framework;
using Observatory.Framework.Interfaces;
using StarGazer.Framework;
using StarGazer.Framework.Interfaces;

namespace StarGazer.Plugins
{
    public class PlaceholderPlugin : IObservatoryNotifier
    {
        public PlaceholderPlugin()
        {
            this.name = "Null Implementation Plugin";
        }

        public string Name => name;

        private string name;

        public string ShortName => name;

        public string Version => string.Empty;

        public PluginUI PluginUI => new PluginUI(PluginUI.UIType.None, null);

        public object Settings { get => null; set { } }

        public NotificationRendering Filter { get; } = 0;

        public void Load(IObservatoryCore observatoryCore)
        { }

        public void Unload()
        { }

        public void OnNotificationEvent(NotificationArgs notificationArgs)
        { }

        public void OnNotificationCancelled(Guid id)
        { }
    }
}
