using System.Text.Json.Serialization;
using Observatory;
using Observatory.Framework.Interfaces;

namespace ObservatoryUI.WPF.Services
{
    public class AppSettings: IAppSettings
    {
        public string AppTheme { get; set; } = "FluentDark";
        public string JournalFolder { get; set; } = "";

        public bool AllowUnsigned { get; set; } = true;

        public WindowBounds MainWindowBounds { get; set; } = new WindowBounds();

        public bool StartMonitor { get; set; } = true;

        public string ExportFolder { get; set; } = "";

        public bool StartReadAll { get; set; } 

        public string ExportStyle { get; set; } = "";

        public bool InbuiltVoiceEnabled { get; set; } = true;
        public int VoiceVolume { get; set; } = 75;
        public string VoiceRate { get; set; } = "";
        public string VoiceName { get; set; } = "";
        public string VoiceStyle { get; set; } = "";
        public string GoogleTextToSpeechApiKey { get; set; } = "";
        public string AzureTextToSpeechApiKey { get; set; } = "";
        public bool VoiceWelcomeMessage { get; set; } = true;

        public bool InbuiltPopupsEnabled {  get; set; } = true;

        public Dictionary<string, double> GridFontSizes { get; set; } = new Dictionary<string, double>();

        [JsonIgnore]
        public string CoreVersion
        {
            get => typeof(ObservatoryCore).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        }

        [JsonIgnore]
        public string PluginStorageFolder
        {
            get => "";
        }

        public AppSettings()
        {
        }

    }
}
