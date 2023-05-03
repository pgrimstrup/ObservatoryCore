using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NetCoreAudio;
using Observatory.Framework;

namespace Observatory.Herald
{
    class HeraldQueueItem
    {
        public NotificationArgs Args;
        public string SelectedVoice;
        public string SelectedStyle;
        public string SelectedRate;
        public byte Volume;
    }

    class HeraldQueue
    {
        private BlockingCollection<HeraldQueueItem> notifications;
        private SpeechRequestManager speechManager;
        private Player audioPlayer;
        private ILogger ErrorLogger;
        private CancellationTokenSource CancellationToken;
        private Thread _thread;

        public HeraldQueue(SpeechRequestManager speechManager, ILogger errorLogger)
        {
            this.speechManager = speechManager;
            notifications = new();
            audioPlayer = new();
            ErrorLogger = errorLogger;
            CancellationToken = new CancellationTokenSource();

            _thread = new Thread(ProcessQueue);
            _thread.IsBackground = true;
            _thread.Start();
        }

        internal void Enqueue(NotificationArgs notification, string selectedVoice, string selectedStyle = "", string selectedRate = "", int volume = 75)
        {
            HeraldQueueItem item = new HeraldQueueItem {
                Args = notification,
                SelectedVoice = selectedVoice,
                SelectedStyle = selectedStyle,
                SelectedRate = selectedRate,
                Volume = (byte)(volume >= 0 && volume <= 100 ? volume : 75)
            };

            // Volume is perceived logarithmically, convert to exponential curve
            // to make perceived volume more in line with value set.
            item.Volume = ((byte)Math.Floor(Math.Pow(item.Volume / 100.0, 2.0) * 100));

            notifications.Add(item);
        }

        public void Cancel()
        {
            CancellationToken.Cancel();
        }

        private void ProcessQueue()
        {
            string? lastTitle = null;
            DateTime lastTitleTime = DateTime.Now;
            CancellationToken cancelToken = CancellationToken.Token;
            while (!Environment.HasShutdownStarted && !cancelToken.IsCancellationRequested)
            {
                if (notifications.TryTake(out var item, 100, cancelToken))
                {
                    try
                    {
                        Task.Run(() => audioPlayer.SetVolume(item.Volume));
                        ErrorLogger.LogDebug("Processing notification: {0} - {1}", item.Args.Title, item.Args.Detail);

                        if (item.Args.Title != null)
                        {
                            if (lastTitle != null && item.Args.Title == lastTitle && DateTime.Now.Subtract(lastTitleTime).TotalSeconds < 30)
                                item.Args.Suppression |= NotificationSuppression.Title;

                            lastTitle = item.Args.Title;
                            lastTitleTime = DateTime.Now;
                        }

                        if (!item.Args.Suppression.HasFlag(NotificationSuppression.Title))
                        {
                            if (!String.IsNullOrEmpty(item.Args.Title) || !String.IsNullOrEmpty(item.Args.TitleSsml))
                            {
                                string filename = string.IsNullOrWhiteSpace(item.Args.TitleSsml)
                                    ? RetrieveAudioToFile(item, item.Args.Title)
                                    : RetrieveAudioSsmlToFile(item, item.Args.TitleSsml);

                                PlayAudioRequestsSequentially(filename);
                            }
                        }

                        if (!item.Args.Suppression.HasFlag(NotificationSuppression.Detail))
                        {
                            if (!String.IsNullOrEmpty(item.Args.Detail) || !String.IsNullOrEmpty(item.Args.DetailSsml))
                            {
                                string filename = string.IsNullOrWhiteSpace(item.Args.DetailSsml)
                                    ? RetrieveAudioToFile(item, item.Args.Detail)
                                    : RetrieveAudioSsmlToFile(item, item.Args.DetailSsml);

                                PlayAudioRequestsSequentially(filename);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogError(ex, $"Failed to fetch/play notification: {item?.Args.Title} - {item?.Args.Detail}");
                    }
                }
            }
        }

        private string RetrieveAudioToFile(HeraldQueueItem item, string text)
        {
            return RetrieveAudioSsmlToFile(item, $"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"\">{System.Security.SecurityElement.Escape(text)}</voice></speak>");
        }

        private string RetrieveAudioSsmlToFile(HeraldQueueItem item, string ssml)
        {
            return speechManager.GetAudioFileFromSsml(ssml, item.SelectedVoice, item.SelectedStyle, item.SelectedRate);
        }

        private void PlayAudioRequestsSequentially(string filename)
        {
            if (!string.IsNullOrWhiteSpace(filename))
            {
                try
                {
                    ErrorLogger.LogDebug($"Playing audio file: {filename}");
                    Task.Run(() => audioPlayer.Play(filename)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogError(ex, $"Failed to play {filename}: {ex.Message}");
                }

                while (audioPlayer.Playing)
                    Thread.Sleep(50);

                // Explicit stop to ensure device is ready for next file.
                // ...hopefully.
                Task.Run(() => audioPlayer.Stop(true)).GetAwaiter().GetResult();
            }
            speechManager.CommitCache();
        }
    }
}
