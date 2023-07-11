﻿using System.Text.Json.Serialization;
using StarGazer.Framework.Interfaces;

namespace StarGazer.UI.Services
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
        public int VoiceRate { get; set; } = 50;
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
            get => typeof(StarGazerCore).Assembly.GetName().Version?.ToString() ?? "0.0.0";
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
