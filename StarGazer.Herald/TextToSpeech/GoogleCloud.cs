﻿using System.Diagnostics;
using System.Net.Http.Json;
using System.Xml;
using Microsoft.Extensions.Logging;
using Observatory.Framework;
using StarGazer.Framework;

namespace StarGazer.Herald.TextToSpeech
{
    internal class GoogleCloud : ITextToSpeechService
    {
        public const string ApiEndPoint = "https://texttospeech.googleapis.com/v1beta1/";
        public const string ApiGetVoices = "voices";
        public const string ApiTextToSpeech = "text:synthesize";
        static readonly char[] LettersAndNumbers = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

        HttpClient _http;
        string _apiKey;
        ILogger _logger;

        string ApiKey => _apiKey ?? throw new Exception("Google Text-to-Speech API Key has not been initialized");

        public GoogleCloud(HttpClient http, string apiKey, ILogger logger)
        {
            _http = http;
            _apiKey = apiKey;
            _logger = logger;
        }

        public async Task<bool> GetTextToSpeechAsync(VoiceNotificationArgs args, string speech, string filename)
        {

            GoogleTextToSpeechRequest request = new GoogleTextToSpeechRequest();
            request.Voice.Name = args.VoiceName;
            request.Voice.LanguageCode = args.VoiceName.Substring(0, 5);
            request.AudioConfig.AudioEncoding = GoogleAudioEncoding.LINEAR16;

            if (float.TryParse(args.VoiceRate, out var rate))
                request.AudioConfig.SpeakingRate = rate;

            if (float.TryParse(args.VoicePitch, out var pitch))
                request.AudioConfig.Pitch = pitch;

            if (speech.StartsWith("<speak"))
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(speech);
                    if(doc.InnerText.IndexOfAny(LettersAndNumbers) < 0)
                    {
                        Debug.WriteLine($"The SSML does not contain any letters or numbers - it cannot be spoken:\r\n{speech}");
                        return false;
                    }

                    request.Input.Ssml = speech;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"The SSML is not valid XML: {ex}:\r\n{speech}");
                    return false;
                }
            }
            else
                request.Input.Text = speech;

            _logger.LogDebug($"Google Text-to-Speech Request: Voice={request.Voice.Name}, Rate={request.AudioConfig.SpeakingRate}, Pitch={request.AudioConfig.Pitch}, Encoding={request.AudioConfig.AudioEncoding}\r\n{speech}");
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