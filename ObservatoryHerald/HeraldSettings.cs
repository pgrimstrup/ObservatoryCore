using Observatory.Framework;
using Observatory.Herald.TextToSpeech;
using System;
using System.Collections.Generic;

namespace Observatory.Herald
{
    public class HeraldSettings
    {
        [SettingIgnoreAttribute]
        internal Func<Dictionary<string, object>> GetVoices;

        [SettingDisplayNameAttribute("API Key Override: ")]
        public string AzureAPIKeyOverride { get; set; }

        [SettingIgnoreAttribute]
        public string ApiEndpoint { get; set; }


        [SettingDisplayNameAttribute("Voice")]
        [SettingBackingValueAttribute("SelectedVoice")]
        [System.Text.Json.Serialization.JsonIgnore]
        public Dictionary<string, object> Voices 
        {
            get => GetVoices();
        }

        [SettingIgnoreAttribute]
        public string SelectedVoice { get; set; }

        [SettingBackingValueAttribute("SelectedRate")]
        [System.Text.Json.Serialization.JsonIgnore]
        public Dictionary<string, object> Rate
        { get => new Dictionary<string, object> 
            {
                {"Slowest", "0.5"},
                {"Slower", "0.75"},
                {"Default", "1.0"},
                {"Faster", "1.25"},
                {"Fastest", "1.5"}
            }; 
        }

        [SettingIgnoreAttribute]
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
