﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Herald.TextToSpeech
{
    internal class GoogleCloud : ITextToSpeechService
    {
        public const string ApiKey = "AIzaSyDw3YQF7W_BvAEXwh8wYJ3AuPujBlsUAMs";
        public const string VoiceListEndPoint = "https://texttospeech.googleapis.com/v1/voices";
        public const string SpeakEndPoint = "";

        HttpClient _http;

        public GoogleCloud(HttpClient http)
        {
            _http = http;
        }

        public Task<FileInfo> GetTextToSpeechAsync(string ssml, string filename)
        {
            return Task.FromResult(new FileInfo(filename));
        }

        public async Task <IEnumerable<Voice>> GetVoicesAsync()
        {
            GoogleVoiceData voiceData = await  _http.GetFromJsonAsync<GoogleVoiceData>($"{VoiceListEndPoint}?key={ApiKey}");

            // Pull out all voices with an English language code
            var englishVoices = voiceData.Voices.Where(v => v.LanguageCodes.Any(lc => lc.StartsWith("en-"))).ToArray();

            return englishVoices
                .OrderBy(v => v.Name)
                .Select(v => new Voice {
                    Language = v.LanguageCodes.FirstOrDefault(lc => lc.StartsWith("en-")),
                    Gender = v.Gender,
                    Name = v.Name,
                    Description = $"{v.Name} ({v.Gender})"
                });
        }
    }
}
