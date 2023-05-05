using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Enumeration;
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

        public HeraldQueue(SpeechRequestManager speechManager, ILogger errorLogger)
        {
            this.speechManager = speechManager;
            notifications = new();
            audioPlayer = new();
            ErrorLogger = errorLogger;
            CancellationToken = new CancellationTokenSource();

            // Fire and Forget the queue processing task
            Task.Run(ProcessQueueAsync);
        }

        internal void Enqueue(NotificationArgs notification)
        {
            HeraldQueueItem item = new HeraldQueueItem {
                Args = notification,
                SelectedVoice = notification.VoiceName,
                SelectedStyle = notification.VoiceStyle,
                SelectedRate = notification.VoiceRate,
                Volume = (byte)(notification.VoiceVolume >= 0 && notification.VoiceVolume <= 100 ? notification.VoiceVolume : 75)
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

        private async Task ProcessQueueAsync()
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
                        await audioPlayer.SetVolume(item.Volume);
                        ErrorLogger.LogDebug("Processing notification: {0} - {1}", item.Args.Title, item.Args.Detail);

                        if (item.Args.Title != null)
                        {
                            if (lastTitle != null && item.Args.Title == lastTitle && DateTime.Now.Subtract(lastTitleTime).TotalSeconds < 30)
                                item.Args.Suppression |= NotificationSuppression.Title;

                            lastTitle = item.Args.Title;
                            lastTitleTime = DateTime.Now;
                        }

                        string filename;
                        if (!item.Args.Suppression.HasFlag(NotificationSuppression.Title))
                        {
                            if (!String.IsNullOrWhiteSpace(item.Args.Title) || !String.IsNullOrWhiteSpace(item.Args.TitleSsml))
                            {
                                if (String.IsNullOrWhiteSpace(item.Args.TitleSsml))
                                    filename = await RetrieveAudioToFileAsync(item, item.Args.Title);
                                else
                                    filename = await RetrieveAudioSsmlToFileAsync(item, item.Args.TitleSsml);

                                Debug.WriteLine($"Herald: Playing {filename}");
                                await PlayAudioRequestsSequentiallyAsync(filename);
                            }
                        }

                        if (!item.Args.Suppression.HasFlag(NotificationSuppression.Detail))
                        {
                            if (!String.IsNullOrWhiteSpace(item.Args.Detail) || !String.IsNullOrWhiteSpace(item.Args.DetailSsml))
                            {
                                if (String.IsNullOrWhiteSpace(item.Args.DetailSsml))
                                    filename = await RetrieveAudioToFileAsync(item, item.Args.Detail);
                                else
                                    filename = await RetrieveAudioSsmlToFileAsync(item, item.Args.DetailSsml);

                                Debug.WriteLine($"Herald: Playing {filename}");
                                await PlayAudioRequestsSequentiallyAsync(filename);
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

        private async Task<string> RetrieveAudioToFileAsync(HeraldQueueItem item, string text)
        {
            return await RetrieveAudioSsmlToFileAsync(item, $"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"\">{System.Security.SecurityElement.Escape(text)}</voice></speak>");
        }

        private async Task<string> RetrieveAudioSsmlToFileAsync(HeraldQueueItem item, string ssml)
        {
            return await speechManager.GetAudioFileFromSsmlAsync(ssml, item.SelectedVoice, item.SelectedStyle, item.SelectedRate);
        }

        private async Task PlayAudioRequestsSequentiallyAsync(string filename)
        {
            if (!string.IsNullOrWhiteSpace(filename))
            {
                try
                {
                    ErrorLogger.LogDebug($"Playing audio file: {filename}");
                    await audioPlayer.Play(filename);
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogError(ex, $"Failed to play {filename}: {ex.Message}");
                }

                while (audioPlayer.Playing)
                    await Task.Delay(50);

                // Explicit stop to ensure device is ready for next file.
                // ...hopefully. Fire and Forget
                _ = audioPlayer.Stop(true);
            }
            speechManager.CommitCache();
        }
    }
}
