using System.Diagnostics;
using System.IO;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Observatory.Framework;
using Observatory.Framework.Interfaces;
using ObservatoryUI.WPF.Services;
using ObservatoryUI.WPF.Views;
using Syncfusion.SfSkinManager;
using Syncfusion.Windows.Tools.Controls;

namespace ObservatoryUI.WPF
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        readonly IObservatoryCoreAsync _core;
        readonly IAppSettings _settings;
        Lazy<IEnumerable<string>> _inbuiltVoiceNames;

        public AppSettings Model { get; set; } = new AppSettings();
        public string CoreVersion => _core.Version;
        public Dictionary<IObservatoryPlugin, object> ActivePlugins { get; set; }

        public SettingsWindow(IObservatoryCoreAsync core, IAppSettings settings)
        {
            _core = core;
            _settings = settings;
            _inbuiltVoiceNames = new Lazy<IEnumerable<string>>(GetInbuiltVoiceNames);

            // Create a list of plugins and the editable copy of their settings
            ActivePlugins = _core.ActivePlugins
                .Where(p => p.Settings != null)
                .OrderBy(p => p.ShortName)
                .ToDictionary(k => k, v => v.Settings.Copy());

            // Copy all properties into the Model instance
            foreach (var prop in typeof(AppSettings).GetProperties().Where(p => p.CanRead && p.CanWrite))
                prop.SetValue(Model, prop.GetValue(_settings));


            SfSkinManager.SetTheme(this, new Theme(settings.AppTheme));
            InitializeComponent();

            DataContext = this;
            foreach (var plugin in ActivePlugins)
            {
                TabItem item = new TabItem();
                item.Header = plugin.Key.ShortName;
                item.Content = new PluginSettingsControl(plugin.Key, plugin.Value);
                SettingsTabControl.Items.Add(item);
            }
        }

        public IEnumerable<string> InbuiltVoiceNames => _inbuiltVoiceNames.Value;

        private IEnumerable<string> GetInbuiltVoiceNames()
        {
            using var speech = new SpeechSynthesizer();
            return speech.GetInstalledVoices().Select(v => v.VoiceInfo.Name).ToList();
        }

        private async void OnTestClicked(object sender, RoutedEventArgs e)
        {
            bool wasEnabled = _settings.InbuiltVoiceEnabled;
            _settings.InbuiltVoiceEnabled = true;

            NotificationArgs args = new NotificationArgs {
                Detail = $"This is a test of the Inbuilt Voice Notification system, using the {Model.VoiceName} voice.",
                Rendering = NotificationRendering.NativeVocal,
                VoiceName = Model.VoiceName,
                VoiceVolume = Model.VoiceVolume,
                VoiceRate = Model.VoiceRate
            };
            await _core.SendNotificationAsync(args);

            _settings.InbuiltVoiceEnabled = wasEnabled;
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OkClicked(object sender, RoutedEventArgs e)
        {
            // Copy all properties back to the settings object and save 
            foreach (var prop in typeof(AppSettings).GetProperties().Where(p => p.CanRead && p.CanWrite))
                prop.SetValue(_settings, prop.GetValue(Model));
            _core.SaveCoreSettings();

            // Simply assign the plugin settings to the plugin and save 
            foreach (var plugin in ActivePlugins.Keys)
            {
                plugin.Settings = ActivePlugins[plugin];
                _core.SavePluginSettings(plugin);
            }

            DialogResult = true;
        }

        private void BrowseJournalFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.InitialDirectory = Model.JournalFolder;
            dlg.FileName = "Journal.log";
            dlg.Filter = "Log Files (*.log)|*.log";
            dlg.ValidateNames = false;
                
            if(dlg.ShowDialog(this) == true)
            {
                Model.JournalFolder = Path.GetDirectoryName(dlg.FileName)!;
            }
        }
    }
}
