using Observatory.Framework;
using Observatory.Herald.TextToSpeech;
using System;
using System.Collections.Generic;

namespace Observatory.Herald
{
    public class HeraldSettings
    {

        [SettingDisplayName("API Key Override")]
        public string AzureAPIKeyOverride { get; set; }

        [SettingIgnore]
        public string ApiEndpoint { get; set; }

        [SettingDisplayName("Voice")]
        [SettingGetItemsMethod(nameof(HeraldNotifier.GetVoiceNames))]
        public string SelectedVoice { get; set; }

        [SettingDisplayName("Voice Rate")]
        [SettingGetItemsMethod(nameof(HeraldNotifier.GetVoiceRates))]
        public string SelectedRate { get; set; }

        [SettingDisplayName("Volume")]
        [SettingNumericUseSlider, SettingNumericBounds(0,100,1)]
        public int Volume { get; set;}

        [System.Text.Json.Serialization.JsonIgnore]
        public Action Test { get; internal set; }

        [SettingDisplayName("Enabled")]
        public bool Enabled { get; set; }

        [SettingDisplayName("Cache Size (MB): ")]
        public int CacheSize { get; set; }
    }
}
