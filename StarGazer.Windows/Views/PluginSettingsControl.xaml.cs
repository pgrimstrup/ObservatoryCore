using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Observatory.Framework.Interfaces;
using StarGazer.Framework;
using StarGazer.Framework.Interfaces;
using StarGazer.Plugins;

namespace StarGazer.UI.Views
{
    /// <summary>
    /// Interaction logic for PluginSettingsControl.xaml
    /// </summary>
    public partial class PluginSettingsControl : UserControl
    {
        public IObservatoryPlugin Plugin { get; set; }
        public object Settings { get; set; } // A copy of the plugin settings

        public ObservableCollection<SettingProperty> SettingProperties { get; } = new ObservableCollection<SettingProperty>();

        public PluginSettingsControl(IObservatoryPlugin plugin, object settings)
        {
            InitializeComponent();

            Plugin = plugin;
            Settings = settings;

            CreateSettingProperties();
            DataContext = this;
        }

        public void CreateSettingProperties()
        {
            SettingProperties.Clear();

            foreach(var prop in SettingProperty.CreateSettingProperties(Plugin, Settings))
            {
                SettingProperties.Add(prop);
            }
        }

        private async void Action_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                if(button.DataContext is SettingProperty property)
                {
                    await property.DoAction();
                }
            }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            var grid = (Grid)sender;
            int MaxGridRow = SettingProperties.Max(p => p.Row);

            while(grid.RowDefinitions.Count <= MaxGridRow)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
        }
    }
}
