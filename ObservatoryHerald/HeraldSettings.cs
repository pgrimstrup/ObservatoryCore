using Observatory.Framework;
using Observatory.Herald.TextToSpeech;
using System;
using System.Collections.Generic;

namespace Observatory.Herald
{
    public class HeraldSettings
    {

        [SettingDisplayNameAttribute("API Key Override")]
        public string AzureAPIKeyOverride { get; set; }

        [SettingIgnoreAttribute]
        public string ApiEndpoint { get; set; }

        [SettingDisplayName("Voice")]
        [SettingGetItemsMethod(nameof(HeraldNotifier.GetVoices))]
        public string SelectedVoice { get; set; }

        [SettingDisplayName("Voice Rate")]
        [SettingGetItemsMethod(nameof(HeraldNotifier.GetVoiceRates))]
        public string SelectedRate { get; set; }

        [SettingDisplayNameAttribute("Volume")]
        [SettingNumericUseSliderAttribute, SettingNumericBoundsAttribute(0,100,1)]
        public int Volume { get; set;}

        [System.Text.Json.Serialization.JsonIgnore]
        public Action Test { get; internal set; }

        [SettingDisplayNameAttribute("Enabled")]
        public bool Enabled { get; set; }

        [SettingDisplayNameAttribute("Cache Size (MB): ")]
        public int CacheSize { get; set; }
    }
}
