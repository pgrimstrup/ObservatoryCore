using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Observatory.Framework;
using Observatory.Framework.Interfaces;

namespace ObservatoryUI.WPF.Views
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
            SettingProperty? previous = null;

            foreach (var property in Settings.GetType().GetProperties())
            {
                var current = new SettingProperty(Plugin, Settings, property, previous);
                if (current.Hidden)
                    continue;

                SettingProperties.Add(current);
                previous = current;
            }
        }

        private void Action_Click(object sender, RoutedEventArgs e)
        {

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
