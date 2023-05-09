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
                BridgeLog log = new BridgeLog(Bridge.Instance);
                log.IsTitleSpoken = AlwaysSpeakTitles;
                log.TitleSsml.Append("Testing Vocalizer");
                log.DetailSsml.Append("Bridge crew standing by,");
                log.DetailSsml.AppendEmphasis("Commander", EmphasisType.Moderate);
                log.DetailSsml.EndSentence();

                var status = Bridge.Instance.CurrentStatus;
                if (status == null)
                {
                    log.DetailSsml.Append("Current ship status is unavailable.");
                }
                else
                {
                    if (status.Flags.HasFlag(StatusFlags.Supercruise))
                        log.DetailSsml.Append("Ship is currently super cruising at").AppendBodyName(status.BodyName).Append(".");
                    else if (status.Flags.HasFlag(StatusFlags.Docked))
                        log.DetailSsml.Append("Ship is docked at").AppendBodyName(status.BodyName).Append(".");
                    else if (status.Flags.HasFlag(StatusFlags.Landed))
                        log.DetailSsml.Append("Ship is on the ground at").AppendBodyName(status.BodyName).Append(".");
                    else
                        log.DetailSsml.Append("Ship is currently idle at").AppendBodyName(status.BodyName).Append(".");

                    log.DetailSsml.Append("Available credit balance is").AppendNumber(status.Balance).Append(".");

                    if (!String.IsNullOrEmpty(status.Destination.Name))
                        log.DetailSsml.Append("Our destination is").AppendBodyName(status.Destination.Name).Append(".");
                }

                Bridge.Instance.LogEvent(log, this);
            }
            catch (Exception ex)
            {
                
            }
        };


    }
}
