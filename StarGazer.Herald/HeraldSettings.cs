using System.ComponentModel;
using StarGazer.Framework;
using StarGazer.Framework.Interfaces;

namespace StarGazer.Herald
{
    public class HeraldSettings 
    {
        [SettingDisplayName("Enabled")]
        public bool Enabled { get; set; }

        [SettingDisplayName("API Key Override")]
        public string APIKeyOverride { get; set; }

        [SettingIgnore]
        public string ApiEndpoint { get; set; }

        [SettingDisplayName("Voice")]
        [SettingGetItemsMethod(nameof(HeraldNotifier.GetVoiceNamesAsync))]
        [SettingDependsOn(nameof(SelectedStyle))]
        public string SelectedVoice { get; set; }

        [SettingDisplayName("Style")]
        [SettingGetItemsMethod(nameof(HeraldNotifier.GetVoiceStyles))]
        public string SelectedStyle { get; set; }

        [SettingDisplayName("Voice Rate")]
        [SettingGetItemsMethod(nameof(HeraldNotifier.GetVoiceRates))]
        public string SelectedRate { get; set; }

        [SettingDisplayName("Volume")]
        [SettingNumericUseSlider, SettingNumericBounds(0,100,1)]
        public int Volume { get; set;}

        [SettingDisplayName("Cache Size (MB)")]
        public int CacheSize { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        [SettingPluginAction(nameof(HeraldNotifier.TestVoiceSettings))]
        [SettingDisplayName("Test Voice")]
        public Action Test { get; internal set; }

        [System.Text.Json.Serialization.JsonIgnore]
        [SettingPluginAction(nameof(HeraldNotifier.ClearVoiceCache))]
        [SettingDisplayName("Clear Cache")]
        public Action Clear { get; internal set; }
    }
}
