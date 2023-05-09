using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Observatory.Herald.TextToSpeech
{
    internal class GoogleVoiceListResponse
    {
        [JsonPropertyName("voices")]
        public GoogleVoiceData[] Voices { get; set; }
    }

    internal class GoogleVoiceData
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

    internal class GoogleTextToSpeechRequest
    {
        [JsonPropertyName("input")]
        public GoogleSynthesisInput Input { get; } = new GoogleSynthesisInput();

        [JsonPropertyName("voice")]
        public GoogleVoiceSelectionParams Voice { get; } = new GoogleVoiceSelectionParams();

        [JsonPropertyName("audioConfig")]
        public GoogleAudioConfig AudioConfig { get; } = new GoogleAudioConfig();
    }

    internal class GoogleSynthesisInput
    {
        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Text { get; set; }

        [JsonPropertyName("ssml")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Ssml { get; set; }
    }

    internal enum GoogleSsmlVoiceGender
    {
        SSML_VOICE_GENDER_UNSPECIFIED,
        MALE,
        FEMALE,
        NEUTRAL
    }

    internal class GoogleVoiceSelectionParams
    {
        [JsonPropertyName("languageCode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string LanguageCode { get; set; }

        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Name { get; set; }

        [JsonPropertyName("ssmlGender")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GoogleSsmlVoiceGender? SsmlGender { get; set; }

        [JsonPropertyName("customVoice")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object CustomVoice { get; set; }
    }

    internal enum GoogleAudioEncoding
    {
        AUDIO_ENCODING_UNSPECIFIED,
        LINEAR16,
        MP3,
        OGG_OPUS,
        MULAW,
        ALAW
    }

    internal class GoogleAudioConfig
    {
        [JsonPropertyName("audioEncoding")]
        public GoogleAudioEncoding AudioEncoding { get; set; }

        [JsonPropertyName("speakingRate")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? SpeakingRate { get; set; }

        [JsonPropertyName("pitch")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? Pitch { get; set; }

        [JsonPropertyName("volumeGainDb")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? VolumeGainDb { get; set; }

        [JsonPropertyName("sampleRateHertz")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SampleRateHertz { get; set; }

        [JsonPropertyName("effectsProfileId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[] EffectsProfileId { get; set; }

    }

    internal class GoogleTextToSpeechResponse
    {
        [JsonPropertyName("audioContent")]
        public byte[] AudioContent { get; set; }
    }
}
