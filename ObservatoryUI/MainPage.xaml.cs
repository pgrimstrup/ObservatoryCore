using System.Collections.ObjectModel;
using Observatory;
using Observatory.Framework.Interfaces;
using ObservatoryUI.Temp;
using ObservatoryUI.ViewModels;
using ObservatoryUI.Views;

namespace ObservatoryUI
{
    public partial class MainPage : ContentPage
    {
        readonly IObservatoryCoreAsync _core;
        readonly IAppSettings _settings;
        readonly ObservableCollection<PluginViewModel> _views;

        public IList<PluginViewModel> Views => _views;

        public MainPage(IObservatoryCoreAsync core, IAppSettings settings)
        {
            InitializeComponent();

            _settings = settings;
            _core = core;
            var plugins = _core.Initialize();

            _views = CreatePluginViews(plugins);
            BindingContext = this;
        }

        private ObservableCollection<PluginViewModel> CreatePluginViews(IEnumerable<IObservatoryPlugin> plugins)
        {
            ObservableCollection<PluginViewModel> models = new ObservableCollection<PluginViewModel>();

            int column = 0;
            int row = 0;

            foreach (var plugin in plugins.Where(p => p.PluginUI?.DataGrid != null))
            {
                PluginView view = new PluginView();
                PluginViewModel viewmodel = new PluginViewModel(plugin, view);

                viewmodel.Column = column++;
                viewmodel.Row = row;

                if(row < MainPageGrid.RowDefinitions.Count)
                    models.Add(viewmodel);

                if (column >= MainPageGrid.ColumnDefinitions.Count)
                {
                    row++;
                    column = 0;
                }
            }

            return models;
        }

    }
}