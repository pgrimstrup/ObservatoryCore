using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework;

namespace Observatory.Herald.TextToSpeech
{
    internal class GoogleCloud : ITextToSpeechService
    {
        public const string ApiKey = "AIzaSyDw3YQF7W_BvAEXwh8wYJ3AuPujBlsUAMs";
        public const string ApiEndPoint = "https://texttospeech.googleapis.com/v1/";
        public const string ApiGetVoices = "voices";
        public const string ApiTextToSpeech = "text:synthesize";

        HttpClient _http;

        public GoogleCloud(HttpClient http)
        {
            _http = http;
        }

        public async Task<bool> GetTextToSpeechAsync(NotificationArgs args, string speech, string filename)
        {
            GoogleTextToSpeechRequest request = new GoogleTextToSpeechRequest();
            request.Voice.Name = args.VoiceName;
            request.Voice.LanguageCode = args.VoiceName.Substring(0, 5);
            request.AudioConfig.AudioEncoding = args.AudioEncoding switch {
                ".wav" => GoogleAudioEncoding.LINEAR16,
                ".ogg" => GoogleAudioEncoding.OGG_OPUS,
                ".mp3" => GoogleAudioEncoding.MP3,
                _ => GoogleAudioEncoding.LINEAR16
            };
            if (float.TryParse(args.VoiceRate, out var rate))
                request.AudioConfig.SpeakingRate = rate;

            if (speech.StartsWith("<speak>"))
                request.Input.Ssml = speech;
            else
                request.Input.Text = speech;

            var response = await _http.PostAsJsonAsync($"{ApiEndPoint}{ApiTextToSpeech}?key={ApiKey}", request);

            response.EnsureSuccessStatusCode();
            var textToSpeech = await response.Content.ReadFromJsonAsync<GoogleTextToSpeechResponse>();
            await File.WriteAllBytesAsync(filename, textToSpeech.AudioContent);

            return true;
        }

        public async Task<IEnumerable<Voice>> GetVoicesAsync()
        {
            var voiceData = await _http.GetFromJsonAsync<GoogleVoiceListResponse>($"{ApiEndPoint}{ApiGetVoices}?key={ApiKey}");

            // Pull out all voices with an English language code
            var englishVoices = voiceData.Voices.Where(v => v.LanguageCodes.Any(lc => lc.StartsWith("en-"))).ToArray();

            return englishVoices
                .OrderBy(v => v.Name)
                .Select(v => new Voice {
                    Language = v.LanguageCodes.FirstOrDefault(lc => lc.StartsWith("en-")),
                    Gender = v.Gender,
                    Name = v.Name,
                    Category = GetCategory(v.Name),
                    Description = GetDescription(v.Name, v.Gender)
                });
        }

        private string GetCategory(string name)
        {
            return name.Split('-').Skip(2).FirstOrDefault();
        }

        private string GetDescription(string name, string gender)
        {
            var parts = name.Split('-');

            string lang = String.Join("-", parts.Take(2));
            string category = parts.Skip(2).FirstOrDefault();
            string id = String.Join("-", parts.Skip(3));

            switch (lang.ToLower())
            {
                case "en-us":
                    lang = "English (US)";
                    break;
                case "en-gb":
                    lang = "English (UK)";
                    break;
                case "en-au":
                    lang = "English (Australia)";
                    break;
                case "en-in":
                    lang = "English (India)";
                    break;
            }

            switch (gender.ToLower())
            {
                case "male":
                    gender = "Male";
                    break;
                case "female":
                    gender = "Female";
                    break;
            }

            return $"{lang} {id}, {gender}";
        }
    }
}
