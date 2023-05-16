using StarGazer.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using StarGazer.Herald.TextToSpeech;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using StarGazer.Framework.Interfaces;
using static System.Formats.Asn1.AsnWriter;
using System.Text.Json.Serialization;

namespace StarGazer.Herald
{
    class SpeechRequestManager
    {
        private DirectoryInfo cacheLocation;
        private int cacheSize;
        private ILogger _logger;
        private ConcurrentDictionary<string, CacheData> cacheIndex;
        private List<Voice> voices;
        private string initialVoice;

        ITextToSpeechService _speech;
        IAudioPlayback _player;

        string CacheIndexFile => Path.Combine(cacheLocation.FullName, "CacheIndex.json");


        internal SpeechRequestManager(IAppSettings appSettings, HeraldSettings settings, HttpClient httpClient, string cacheFolder, ILogger logger, IAudioPlayback player)
        {
            string apiKey = appSettings.GoogleTextToSpeechApiKey;
            if(!String.IsNullOrWhiteSpace(settings.APIKeyOverride))
                apiKey = settings.APIKeyOverride;

            _speech = new GoogleCloud(httpClient, apiKey, logger);
            _player = player;

            cacheSize = Math.Max(settings.CacheSize, 1);
            cacheLocation = new DirectoryInfo(cacheFolder);
            if (!cacheLocation.Exists)
                cacheLocation.Create();

            ReadCache();
            _logger = logger;

            initialVoice = settings.SelectedVoice;
        }

        internal async Task<FileInfo> GetAudioFileFromSsmlAsync(VoiceNotificationArgs args, string speech)
        {
            using var sha = SHA256.Create();

            // Create a string based on the SSML and provided parameters. Calculate the hash based on this.
            var uniqueness = $"{speech}|{args.VoiceName}|{args.VoiceRate}|{args.VoiceVolume}|{args.VoiceStyle}";
            var hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(uniqueness))).Replace("-", string.Empty);
            var audioFilename = Path.Combine(cacheLocation.FullName, hash + ".opus");

            FileInfo audioFileInfo = new FileInfo(audioFilename);
            if (audioFileInfo.Exists)
            {
                if (audioFileInfo.Length == 0)
                {
                    audioFileInfo.Delete();
                    audioFileInfo = null;
                }
            }


            if (audioFileInfo == null || !audioFileInfo.Exists)
            {
                try
                {
                    // Always grab text-to-speech as WAV file (PCM)
                    // Google's conversion to Ogg Opus is really bad, almost as bad as MP3. So I'm going
                    // to avoid using MP3 and OGG from Google and simply use WAV. The NAudio encoder encodes
                    // to Ogg Opus really well, so we will do it ourselves.
                    string waveFilename = Path.ChangeExtension(audioFilename, ".wav");
                    if (await _speech.GetTextToSpeechAsync(args, speech, waveFilename))
                    {
                        // Convert to Ogg Opus to reduce size, keeping quality pretty good.
                        var opusFilename = _player.ConvertWavToOpus(waveFilename);
                        File.Delete(waveFilename);

                        audioFileInfo = new FileInfo(opusFilename);
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "while processing text-to-speech");
                    return null;
                }
            }

            if(audioFileInfo != null)
                UpdateAndPruneCache(audioFileInfo);

            return audioFileInfo;
        }

        private static string AddVoiceToSsml(string ssml, string voiceName, string styleName, string rate)
        {
            XmlDocument ssmlDoc = new();
            ssmlDoc.LoadXml(ssml);

            var ssmlNamespace = ssmlDoc.DocumentElement.NamespaceURI;
            XmlNamespaceManager ssmlNs = new(ssmlDoc.NameTable);
            ssmlNs.AddNamespace("ssml", ssmlNamespace);
            ssmlNs.AddNamespace("mstts", "http://www.w3.org/2001/mstts");
            ssmlNs.AddNamespace("emo", "http://www.w3.org/2009/10/emotionml");

            var voiceNode = ssmlDoc.SelectSingleNode("/ssml:speak/ssml:voice", ssmlNs);
            voiceNode.Attributes.GetNamedItem("name").Value = voiceName;

            if (!string.IsNullOrWhiteSpace(rate))
            {
                var prosodyNode = ssmlDoc.CreateElement("ssml", "prosody", ssmlNamespace);
                prosodyNode.SetAttribute("rate", rate);
                prosodyNode.InnerXml = voiceNode.InnerXml;
                voiceNode.InnerXml = prosodyNode.OuterXml;
            }

            if (!string.IsNullOrWhiteSpace(styleName))
            {
                var expressAsNode = ssmlDoc.CreateElement("mstts", "express-as", "http://www.w3.org/2001/mstts");
                expressAsNode.SetAttribute("style", styleName);
                expressAsNode.InnerXml = voiceNode.InnerXml;
                voiceNode.InnerXml = expressAsNode.OuterXml;
            }

            return ssmlDoc.OuterXml;
        }

        internal async Task<List<Voice>> GetVoices()
        {
            if (voices != null)
                return voices;

            try
            {
                var result = await _speech.GetVoicesAsync();
                voices = result.ToList();
                return voices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "When retrieving a list of available voices from the server");

                // Return the last known voice
                var result = new List<Voice>();
                result.Add(new Voice(initialVoice));
                return result;
            }
        }

        private List<FileInfo> GetCacheFiles()
        {
            var cacheFiles = cacheLocation.GetFiles("*.mp3")
                .Union(cacheLocation.GetFiles("*.ogg"))
                .Union(cacheLocation.GetFiles("*.opus"))
                .Union(cacheLocation.GetFiles("*.wav"))
                .ToList();

            // Remove all files with a period at the start of their name
            cacheFiles.RemoveAll(f => f.Name.StartsWith("."));

            return cacheFiles;
        }

        private void ReadCache()
        {

            if (File.Exists(CacheIndexFile))
            {
                var indexFileContent = File.ReadAllText(CacheIndexFile);
                try
                {
                    cacheIndex = JsonSerializer.Deserialize<ConcurrentDictionary<string, CacheData>>(indexFileContent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    cacheIndex = new();
                    _logger.LogError(ex, "deserializing CacheIndex.json");
                }
            }
            else
            {
                cacheIndex = new();
            }

            // Re-index orphaned files in event of corrupted or lost index.
            var cacheFiles = GetCacheFiles();
            foreach (var file in cacheFiles.Where(file => !cacheIndex.ContainsKey(file.Name)))
            {
                // Assume orphaned files were last used at their creation time
                cacheIndex.TryAdd(file.Name, new(file.CreationTime, file.CreationTime, 0));
            };
        }

        private void UpdateAndPruneCache(FileInfo currentFile)
        {
            var cacheFiles = GetCacheFiles();

            if (cacheIndex.TryGetValue(currentFile.Name, out var data))
            {
                data.HitCount++;
                data.LastUsed = DateTime.Now;
            }
            else
            {
                cacheIndex.TryAdd(currentFile.Name, new(DateTime.Now, DateTime.Now, 1));
            }

            var indexedCacheSize = cacheFiles.Sum(f => f.Length);
            while (indexedCacheSize > cacheSize * 1024 * 1024)
            {
                var staleFile = (from file in cacheIndex
                                 orderby file.Value.Age descending, file.Value.HitCount
                                 select file.Key).First();

                if (staleFile == currentFile.Name)
                    break;

                cacheIndex.TryRemove(staleFile, out _);
                indexedCacheSize -= staleFile.Length;
                if(File.Exists(staleFile))
                    File.Delete(staleFile);
            }
        }

        internal async void CommitCache()
        {
            System.Diagnostics.Stopwatch stopwatch = new();
            stopwatch.Start();

            // Race condition isn't a concern anymore, but should check this anyway to be safe.
            // (Maybe someone is poking at the file with notepad?)
            while (!IsFileWritable(CacheIndexFile) && stopwatch.ElapsedMilliseconds < 1000)
                await Task.Delay(100); // Task.Factory.StartNew(() => System.Threading.Thread.Sleep(100));

            // 1000ms should be more than enough for a conflicting title or detail to complete,
            // if we're still waiting something else is locking the file, just give up.
            if (stopwatch.ElapsedMilliseconds < 1000)
            {
                File.WriteAllText(CacheIndexFile, JsonSerializer.Serialize(cacheIndex, new JsonSerializerOptions { WriteIndented = true}));
            }

            stopwatch.Stop();
        }

        private static bool IsFileWritable(string path)
        {
            try
            {
                using FileStream fs = File.OpenWrite(path);
                fs.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal void ClearCache()
        {
            foreach (var file in GetCacheFiles())
                file.Delete();

            cacheIndex.Clear();
            CommitCache();
        }

        private class CacheData
        {
            public CacheData()
            {

            }

            public CacheData(DateTime Created, DateTime LastUsed, int HitCount)
            {
                this.Created = Created;
                this.LastUsed = LastUsed;
                this.HitCount = HitCount;
            }

            public DateTime Created { get; set; }
            public DateTime LastUsed { get; set; }
            public int HitCount { get; set; }

            [JsonIgnore]
            public int Age => (int)DateTime.Today.Subtract(LastUsed.Date).TotalDays;
        }
    }
}
