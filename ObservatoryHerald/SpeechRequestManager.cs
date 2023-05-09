﻿using Observatory.Framework;
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
using System.Net.Http.Headers;
using Observatory.Framework.Interfaces;

namespace Observatory.Herald
{
    class SpeechRequestManager
    {
        private DirectoryInfo cacheLocation;
        private int cacheSize;
        private ILogger ErrorLogger;
        private ConcurrentDictionary<string, CacheData> cacheIndex;
        private List<Voice> voices;
        private string initialVoice;

        ITextToSpeechService _speech;

        string CacheIndexFile => Path.Combine(cacheLocation.FullName, "CacheIndex.json");


        internal SpeechRequestManager(IAppSettings appSettings, HeraldSettings settings, HttpClient httpClient, string cacheFolder, ILogger errorLogger)
        {
            string apiKey = appSettings.GoogleTextToSpeechApiKey;
            if(!String.IsNullOrWhiteSpace(settings.APIKeyOverride))
                apiKey = settings.APIKeyOverride;

            _speech = new GoogleCloud(httpClient, apiKey);

            cacheSize = Math.Max(settings.CacheSize, 1);
            cacheLocation = new DirectoryInfo(cacheFolder);
            if (!cacheLocation.Exists)
                cacheLocation.Create();

            ReadCache();
            ErrorLogger = errorLogger;

            initialVoice = settings.SelectedVoice;
        }

        internal async Task<FileInfo> GetAudioFileFromSsmlAsync(NotificationArgs args, string speech)
        {
            using var sha = SHA256.Create();

            // Create a string based on the SSML and provided parameters. Calculate the hash based on this.
            var uniqueness = $"{speech}|{args.VoiceName}|{args.VoiceRate}|{args.VoiceVolume}|{args.VoiceStyle}";
            var hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(uniqueness))).Replace("-", string.Empty);
            var audioFilename = Path.Combine(cacheLocation.FullName, hash + args.AudioEncoding);

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
                    if (await _speech.GetTextToSpeechAsync(args, speech, audioFilename))
                        audioFileInfo = new FileInfo(audioFilename);
                }
                catch(Exception ex)
                {
                    ErrorLogger.LogError(ex, "while processing text-to-speech");
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
                ErrorLogger.LogError(ex, "When retrieving a list of available voices from the server");

                // Return the last known voice
                var result = new List<Voice>();
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
