using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Observatory.Framework;
using Observatory.Framework.Files.ParameterTypes;

namespace Observatory.Bridge
{
    internal class BridgeSettings
    {
        [SettingDisplayName("Enable Bridge Crew")]
        public bool BridgeEnabled { get; set; } = true;

        [SettingDisplayName("Use Herald Vocalizer Plugin")]
        public bool UseHeraldVocalizer { get; set; } = true;

        [SettingDisplayName("Use Internal Vocalizer Plugin")]
        public bool UseInternalVocalizer { get; set; } = false;

        [SettingDisplayName("Always Speak Titles")]
        public bool AlwaysSpeakTitles { get; set; } = false;

        [SettingDisplayName("High Value Body Threshold")]
        [SettingNumericBounds(0, 1000000, 10000)]
        public int HighValueBody { get; set; } = 400000;

        [SettingDisplayName("Spoken Comma Delay")]
        [SettingNumericBounds(0, 5000, 10)]
        public int SpokenCommaDelay { get; set; } = 250;

        [SettingDisplayName("Spoken Period Delay")]
        [SettingNumericBounds(0, 5000, 10)]
        public int SpokenPeriodDelay { get; set; } = 500;

        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        [SettingDisplayName("Test Vocalizer")]
        public Action Test => () => {
            try
            {
                BridgeLog log = new BridgeLog();
                log.IsTitleSpoken = AlwaysSpeakTitles;
                log.TitleSsml.Append("Testing Vocalizer");
                log.DetailSsml.Append("Bridge crew standing by,");
                log.DetailSsml.AppendEmphasis("Commander", EmphasisType.Moderate);
                log.DetailSsml.EndSentence();

                Bridge.Instance.LogEvent(log, this);
            }
            catch (Exception ex)
            {
                
            }
        };


    }
}
