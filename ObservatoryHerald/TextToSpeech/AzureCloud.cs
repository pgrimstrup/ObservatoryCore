using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework;
using static System.Net.WebRequestMethods;
using System.Text.Json;

namespace Observatory.Herald.TextToSpeech
{
    internal class AzureVoice : IVoice
    {
        public string Language { get; set; }

        public string Name { get; set; }

        public string Gender { get; set; }

        public string Description { get; set; }

        public AzureVoice(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    internal class AzureCloud
    {
        public string ApiKey;
        public string ApiEndpoint;
        public HttpClient Http;

        public AzureCloud()
        {

        }

        public AzureCloud(HttpClient httpClient, string apiKey, string apiEndpoint)
        {
            Http = httpClient;
            ApiKey = apiKey;
            ApiEndpoint = apiEndpoint;
        }

        public async Task<IEnumerable<IVoice>> GetVoicesAsync()
        {
            List<IVoice> voices = new List<IVoice>();

            using var request = new HttpRequestMessage(HttpMethod.Get, ApiEndpoint + "/List") {
                Headers = {
                    { "obs-plugin", "herald" },
                    { "api-key", ApiKey }
                }
            };

            CancellationTokenSource cancel = new CancellationTokenSource();
            cancel.CancelAfter(1000);
            var response = await Http.SendAsync(request, cancel.Token);
            if (!response.IsSuccessStatusCode)
                throw new PluginException("Herald", "Unable to retrieve available voices.", new Exception(response.StatusCode.ToString() + ": " + response.ReasonPhrase));

            var voiceJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            var englishSpeakingVoices = from v in voiceJson.RootElement.EnumerateArray()
                                        where v.GetProperty("Locale").GetString().StartsWith("en-")
                                        select v;

            foreach (var voice in englishSpeakingVoices)
            {
                string demonym = GetDemonymFromLocale(voice.GetProperty("Locale").GetString());

                voices.Add(new AzureVoice(
                    voice.GetProperty("ShortName").GetString(),
                    demonym + " - " + voice.GetProperty("LocalName").GetString()));

                if (voice.TryGetProperty("StyleList", out var styles))
                {
                    foreach (var style in styles.EnumerateArray())
                    {
                        voices.Add(new AzureVoice(
                            voice.GetProperty("ShortName").GetString(),
                            demonym + " - " + voice.GetProperty("LocalName").GetString() + " - " + style.GetString()));
                    }
                }
            }

            return voices;
        }

        public async Task<FileInfo> GetTextToSpeech(string ssml, string audioFilename)
        {
            using StringContent request = new(ssml) {
                Headers = {
                        { "obs-plugin", "herald" },
                        { "api-key", ApiKey }
                    }
            };

            CancellationTokenSource cancel = new CancellationTokenSource();
            cancel.CancelAfter(5000);

            using var response = await Http.PostAsync(ApiEndpoint + "/Speak", request, cancel.Token);
            if (response.IsSuccessStatusCode)
            {
                using Stream responseStream = await response.Content.ReadAsStreamAsync();
                using FileStream fileStream = new FileStream(audioFilename, FileMode.CreateNew);

                await responseStream.CopyToAsync(fileStream);
                fileStream.Close();
            }
            else
            {
                throw new PluginException("Herald", "Unable to retrieve audio data.", new Exception(response.StatusCode.ToString() + ": " + response.ReasonPhrase));
            }

            return new FileInfo(audioFilename);
        }

        private static string GetDemonymFromLocale(string locale)
        {
            string demonym;

            switch (locale)
            {
                case "en-AU":
                    demonym = "Australian";
                    break;
                case "en-CA":
                    demonym = "Canadian";
                    break;
                case "en-GB":
                    demonym = "British";
                    break;
                case "en-HK":
                    demonym = "Hong Konger";
                    break;
                case "en-IE":
                    demonym = "Irish";
                    break;
                case "en-IN":
                    demonym = "Indian";
                    break;
                case "en-KE":
                    demonym = "Kenyan";
                    break;
                case "en-NG":
                    demonym = "Nigerian";
                    break;
                case "en-NZ":
                    demonym = "Kiwi";
                    break;
                case "en-PH":
                    demonym = "Filipino";
                    break;
                case "en-SG":
                    demonym = "Singaporean";
                    break;
                case "en-TZ":
                    demonym = "Tanzanian";
                    break;
                case "en-US":
                    demonym = "American";
                    break;
                case "en-ZA":
                    demonym = "South African";
                    break;
                default:
                    demonym = locale;
                    break;
            }

            return demonym;
        }

    }
}
