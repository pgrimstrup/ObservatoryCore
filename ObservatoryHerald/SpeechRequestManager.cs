using Observatory.Framework;
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
using Observatory.Herald.TextToSpeech;
using Microsoft.Extensions.Logging;

namespace Observatory.Herald
{
    class SpeechRequestManager
    {
        private DirectoryInfo cacheLocation;
        private int cacheSize;
        private ILogger ErrorLogger;
        private ConcurrentDictionary<string, CacheData> cacheIndex;
        private List<IVoice> voices;
        private string initialVoice;

        ITextToSpeechService _speech;

        string CacheIndexFile => Path.Combine(cacheLocation.FullName, "CacheIndex.json");


        internal SpeechRequestManager(
            HeraldSettings settings, HttpClient httpClient, string cacheFolder, ILogger errorLogger)
        {
            _speech = new GoogleCloud(httpClient);

            cacheSize = Math.Max(settings.CacheSize, 1);
            cacheLocation = new DirectoryInfo(cacheFolder);
            if (!cacheLocation.Exists)
                cacheLocation.Create();

            ReadCache();
            ErrorLogger = errorLogger;

            initialVoice = settings.SelectedVoice;
        }

        internal string GetAudioFileFromSsml(string ssml, string voice, string style, string rate)
        {

            ssml = AddVoiceToSsml(ssml, voice, style, rate);

            using var sha = SHA256.Create();

            var ssmlHash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(ssml))).Replace("-", string.Empty);

            var audioFilename = Path.Combine(cacheLocation.FullName, ssmlHash + ".mp3");

            FileInfo audioFileInfo = null;
            if (File.Exists(audioFilename))
            {
                audioFileInfo = new FileInfo(audioFilename);
                if (audioFileInfo.Length == 0)
                {
                    File.Delete(audioFilename);
                    audioFileInfo = null;
                }
            }


            if (audioFileInfo == null)
            {
                try
                {
                    audioFileInfo = _speech.GetTextToSpeech(ssml, audioFilename);
                }
                catch(Exception ex)
                {
                    ErrorLogger.LogError(ex, "while processing text-to-speech");
                }
            }

            if(audioFileInfo != null)
                UpdateAndPruneCache(audioFileInfo);

            return audioFilename;
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

        internal List<IVoice> GetVoices()
        {
            if (voices != null)
                return voices;

            try
            {
                voices = _speech.GetVoices().ToList();
                return voices;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError(ex, "When retrieving a list of available voices from the server");

                // Return the last known voice
                var result = new List<IVoice>();
                result.Add(new Voice(initialVoice));
                return result;
            }
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
                    ErrorLogger.LogError(ex, "deserializing CacheIndex.json");
                }
            }
            else
            {
                cacheIndex = new();
            }

            // Re-index orphaned files in event of corrupted or lost index.
            var cacheFiles = cacheLocation.GetFiles("*.mp3")
                .Union(cacheLocation.GetFiles("*.wav"));
            foreach (var file in cacheFiles.Where(file => !cacheIndex.ContainsKey(file.Name)))
            {
                cacheIndex.TryAdd(file.Name, new(file.CreationTime, 0));
            };
        }

        private void UpdateAndPruneCache(FileInfo currentFile)
        {
            var cacheFiles = cacheLocation.GetFiles("*.mp3")
                .Union(cacheLocation.GetFiles("*.wav"));
            if (cacheIndex.ContainsKey(currentFile.Name))
            {
                cacheIndex[currentFile.Name].HitCount++;
            }
            else
            {
                cacheIndex.TryAdd(currentFile.Name, new(DateTime.UtcNow, 1));
            }

            var indexedCacheSize = cacheFiles
                .Where(f => cacheIndex.ContainsKey(f.Name))
                .Sum(f => f.Length);

            while (indexedCacheSize > cacheSize * 1024 * 1024)
            {
                var staleFile = (from file in cacheIndex
                                 orderby file.Value.HitCount, file.Value.Created
                                 select file.Key).First();

                if (staleFile == currentFile.Name)
                    break;

                cacheIndex.TryRemove(staleFile, out _);
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
                File.WriteAllText(CacheIndexFile, JsonSerializer.Serialize(cacheIndex));
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

        private class CacheData
        {
            public CacheData(DateTime Created, int HitCount)
            {
                this.Created = Created;
                this.HitCount = HitCount;
            }
            public DateTime Created { get; set; }
            public int HitCount { get; set; }
        }
    }
}
