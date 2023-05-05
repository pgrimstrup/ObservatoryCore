using System.ComponentModel;
using Observatory.Framework;
using Observatory.Framework.Interfaces;

namespace Observatory.Herald
{
    public class HeraldSettings 
    {

        [SettingDisplayName("API Key Override")]
        public string APIKeyOverride { get; set; }

        [SettingIgnore]
        public string ApiEndpoint { get; set; }

        [SettingDisplayName("Voice")]
        [SettingGetItemsMethod(nameof(HeraldNotifier.GetVoiceNamesAsync))]
        [SettingDependsOn(nameof(SelectedStyle))]
        public string SelectedVoice { get; set; }

        [SettingDisplayName("Style")]
        [SettingGetItemsMethod(nameof(HeraldNotifier.GetVoiceStylesAsync))]
        public string SelectedStyle { get; set; }

        [SettingDisplayName("Voice Rate")]
        [SettingGetItemsMethod(nameof(HeraldNotifier.GetVoiceRates))]
        public string SelectedRate { get; set; }

        [SettingDisplayName("Volume")]
        [SettingNumericUseSlider, SettingNumericBounds(0,100,1)]
        public int Volume { get; set;}

        [System.Text.Json.Serialization.JsonIgnore]
        [SettingPluginAction(nameof(HeraldNotifier.TestVoiceSettings))]
        public Action Test { get; internal set; }

        [SettingDisplayName("Enabled")]
        public bool Enabled { get; set; }

        [SettingDisplayName("Cache Size (MB): ")]
        public int CacheSize { get; set; }
    }
}
