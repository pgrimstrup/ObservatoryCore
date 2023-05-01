using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Xml;
using Observatory.Framework.Interfaces;

namespace ObservatoryUI.WPF.Services
{

    public class VoiceNotificationQueue : IVoiceNotificationQueue
    {
        BlockingCollection<VoiceMessage> _queue = new BlockingCollection<VoiceMessage>();
        VoiceMessage? _current = null;
        Thread _thread;
        CancellationTokenSource _cancel = new CancellationTokenSource();
        IAppSettings _settings;

        public VoiceNotificationQueue(IAppSettings settings)
        {
            _settings = settings;

            _thread = new Thread(Run);
            _thread.IsBackground = true;
            _thread.Start();
        }

        public void Add(VoiceMessage msg)
        {
            _queue.Add(msg);
        }

        public void Update(VoiceMessage msg)
        {
            // Update queued messages with the same ID. We do not update the currently playing message.
            // If the ID is not found, then ignore.
            foreach(var vm in _queue.ToArray())
            {
                if(vm.Id == msg.Id)
                {
                    vm.Title = msg.Title;
                    vm.Detail = msg.Detail;
                }
            }
        }

        public void Cancel(Guid id)
        {
            // Cancel the currently playing message if it has the same ID
            var current = _current;
            if (current != null && current.Id == id)
                current.Cancelled = true;

            // Cancel any queued message with the same ID
            foreach (var msg in _queue.ToArray())
            {
                if (msg.Id == id)
                    msg.Cancelled = true;
            }
        }

        public void Run()
        {
            CancellationToken cancel = _cancel.Token;

            using var speech = new SpeechSynthesizer();
            while (!cancel.IsCancellationRequested)
            {
                if(_queue.TryTake(out VoiceMessage msg, 100, cancel))
                {
                    _current = msg;
                    if (msg.Cancelled)
                        continue;

                    try
                    {
                        string voice = _settings.VoiceName ?? speech.GetInstalledVoices().First().VoiceInfo.Name;

                        speech.Volume = _settings.VoiceVolume;
                        speech.Rate = _settings.VoiceRate;
                        speech.SelectVoice(voice);

                        Speak(speech, msg, UpdateSsmlVoice(msg.Title, voice), cancel);
                        Speak(speech, msg, UpdateSsmlVoice(msg.Detail, voice), cancel);

                    }
                    catch (Exception ex)
                    {

                    }
                    finally
                    {
                        _current = null;
                    }
                }
            }
        }

        private void Speak(SpeechSynthesizer speech, VoiceMessage msg, string ssml, CancellationToken cancel)
        {
            if (!String.IsNullOrEmpty(ssml))
            {
                Prompt p = speech.SpeakSsmlAsync(ssml);
                while (!p.IsCompleted && !cancel.IsCancellationRequested && !msg.Cancelled)
                    cancel.WaitHandle.WaitOne(50);

                if (!p.IsCompleted)
                    speech.SpeakAsyncCancel(p);
            }
        }

        string UpdateSsmlVoice(string ssml, string voiceName)
        {
            try
            {
                XmlDocument ssmlDoc = new();
                ssmlDoc.LoadXml(ssml);

                var ssmlNamespace = ssmlDoc.DocumentElement.NamespaceURI;
                XmlNamespaceManager ssmlNs = new(ssmlDoc.NameTable);
                ssmlNs.AddNamespace("ssml", ssmlNamespace);

                var voiceNode = ssmlDoc.SelectSingleNode("/ssml:speak/ssml:voice", ssmlNs);
                if(voiceNode != null)
                    voiceNode.Attributes.GetNamedItem("name").Value = voiceName;

                return ssmlDoc.OuterXml;
            }
            catch
            {
                return ssml;
            }
        }

        public void Shutdown()
        {
            _cancel.Cancel();
        }
    }
}
