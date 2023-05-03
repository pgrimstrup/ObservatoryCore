using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Observatory.Herald.TextToSpeech
{
    public class GoogleVoiceData
    {
        [JsonPropertyName("voices")]
        public VoiceData[] Voices { get; set; }
    }

    public class VoiceData
    {
        [JsonPropertyName("languageCodes")]
        public string[] LanguageCodes { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("ssmlGender")]
        public string Gender { get; set; }

        [JsonPropertyName("naturalSampleRateHertz")]
        public int NaturalSampleRateHertz { get; set; }
    }
}
