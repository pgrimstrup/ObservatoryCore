using System.Collections.Concurrent;
using System.Speech.Synthesis;
using Microsoft.Extensions.Logging;
using StarGazer.Framework;
using StarGazer.Framework.Interfaces;

namespace StarGazer.UI.Services
{

    public class InbuiltVoiceNotification : IVoiceNotificationQueue
    {
        ILogger _logger;
        BlockingCollection<VoiceNotificationArgs> _queue = new BlockingCollection<VoiceNotificationArgs>();
        VoiceNotificationArgs? _current = null;
        Thread _thread;
        CancellationTokenSource _cancel = new CancellationTokenSource();

        public InbuiltVoiceNotification(ILogger<InbuiltVoiceNotification> logger)
        {
            _logger = logger;
            _thread = new Thread(Run);
            _thread.IsBackground = true;
            _thread.Start();
        }

        public void Add(VoiceNotificationArgs msg)
        {
            _queue.Add(msg);
        }

        public void Update(VoiceNotificationArgs msg)
        {
            // Update queued messages with the same ID. We do not update the currently playing message.
            // If the ID is not found, then ignore.
            foreach(var vm in _queue.ToArray())
            {
                if(vm.Id == msg.Id)
                {
                    vm.Title = msg.Title;
                    vm.TitleSsml = msg.TitleSsml;
                    vm.Detail = msg.Detail;
                    vm.DetailSsml = msg.DetailSsml;
                    vm.IsCancelled = msg.IsCancelled;
                    vm.Suppression = msg.Suppression;
                    vm.Rendering = msg.Rendering;
                    vm.VoiceName = msg.VoiceName;
                    vm.VoiceRate = msg.VoiceRate;
                    vm.VoiceStyle = msg.VoiceStyle;
                    vm.VoiceVolume = msg.VoiceVolume;
                }
            }
        }

        public void Cancel(Guid id)
        {
            // Cancel the currently playing message if it has the same ID
            var current = _current;
            if (current != null && current.Id == id)
                current.IsCancelled = true;

            // Cancel any queued message with the same ID
            foreach (var msg in _queue.ToArray())
            {
                if (msg.Id == id)
                    msg.IsCancelled = true;
            }
        }

        public void Run()
        {
            CancellationToken cancel = _cancel.Token;

            using var speech = new SpeechSynthesizer();
            while (!cancel.IsCancellationRequested)
            {
                if(_queue.TryTake(out VoiceNotificationArgs? msg, 100, cancel))
                {
                    _current = msg;
                    if (msg.IsCancelled)
                        continue;

                    try
                    {
                        speech.Volume = msg.VoiceVolume.GetValueOrDefault(75);
                        // Voice rate is supported, Voice pitch is not supported
                        // Convert value of 0 to 100 to the expected rate for the SpeechSynthesizer, -10 to 10
                        speech.Rate =  (msg.VoiceRate.GetValueOrDefault(50) - 50) / 5;
                        speech.SelectVoice(msg.VoiceName);

                        Speak(speech, msg, msg.TitleSsml, cancel);
                        Speak(speech, msg, msg.DetailSsml, cancel);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "When processing a voice message");
                    }
                    finally
                    {
                        _current = null;
                    }
                }
            }
        }

        private void Speak(SpeechSynthesizer speech, VoiceNotificationArgs msg, string ssml, CancellationToken cancel)
        {
            if (!String.IsNullOrEmpty(ssml))
            {
                _logger.LogDebug($"Inbuilt Voice Notification: {ssml}");
                Prompt p = speech.SpeakSsmlAsync(ssml);
                while (!p.IsCompleted && !cancel.IsCancellationRequested && !msg.IsCancelled)
                    cancel.WaitHandle.WaitOne(50);

                if (!p.IsCompleted)
                    speech.SpeakAsyncCancel(p);

                _logger.LogDebug("Inbuilt Voice Notification: completed ok.");
            }
        }

        //string UpdateSsmlVoice(string ssml, string voiceName)
        //{
        //    try
        //    {
        //        if (String.IsNullOrEmpty(ssml))
        //            return "";

        //        XmlDocument ssmlDoc = new();
        //        ssmlDoc.LoadXml(ssml);
        //        if (ssmlDoc.DocumentElement != null)
        //        {
        //            var ssmlNamespace = ssmlDoc.DocumentElement.NamespaceURI;
        //            XmlNamespaceManager ssmlNs = new(ssmlDoc.NameTable);
        //            ssmlNs.AddNamespace("ssml", ssmlNamespace);

        //            var voiceNode = ssmlDoc.SelectSingleNode("/ssml:speak/ssml:voice", ssmlNs);
        //            if (voiceNode != null && voiceNode.Attributes?.GetNamedItem("name") is XmlAttribute attrib)
        //                attrib.Value = voiceName;
        //        }

        //        return ssmlDoc.OuterXml;
        //    }
        //    catch(Exception ex)
        //    {
        //        _logger.LogError(ex, "When updating the SSML Voice attribute");
        //        return ssml;
        //    }
        //}

        public void Shutdown()
        {
            _cancel.Cancel();
        }
    }
}
