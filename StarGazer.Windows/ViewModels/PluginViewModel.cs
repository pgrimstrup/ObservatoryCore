using Observatory.Framework.Interfaces;
using StarGazer.Framework;
using StarGazer.UI.Views;

namespace StarGazer.UI.ViewModels
{
    public class PluginViewModel 
    {
        readonly IObservatoryPlugin _plugin;
        readonly PluginView _view;
        readonly PluginUIWrapper _wrapper;

        public int Row { get; set; }
        public int Column { get; set; }
        public PluginView View => _view;
        public IObservatoryPlugin Plugin => _plugin;

        public PluginViewModel(IObservatoryPlugin plugin, PluginView view)
        {
            _plugin = plugin;
            _view = view;
            _view.Name = plugin.GetType().FullName!.Replace(".", "_");
            _view.DataContext = _wrapper = new PluginUIWrapper(_plugin.PluginUI);
        }

    }
}
