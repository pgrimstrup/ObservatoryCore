using Observatory.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NetCoreAudio;
using System.Threading;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using Observatory.Framework.Files.ParameterTypes;
using Microsoft.Extensions.Logging;

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
        private Task queueTask;
        private CancellationTokenSource CancellationToken;

        public HeraldQueue(SpeechRequestManager speechManager, ILogger errorLogger)
        {
            this.speechManager = speechManager;
            notifications = new();
            audioPlayer = new();
            ErrorLogger = errorLogger;
            CancellationToken = new CancellationTokenSource();
            ProcessQueueAsync();
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

        private void ProcessQueueAsync()
        {
            queueTask = Task.Run(ProcessQueue);
        }

        public void Cancel()
        {
            CancellationToken.Cancel();
        }

        private async Task ProcessQueue()
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

                        List<Task<string>> audioRequestTasks = new();

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
                                audioRequestTasks.Add(string.IsNullOrWhiteSpace(item.Args.TitleSsml)
                                    ? RetrieveAudioToFile(item, item.Args.Title)
                                    : RetrieveAudioSsmlToFile(item, item.Args.TitleSsml));

                            }
                        }

                        if (!item.Args.Suppression.HasFlag(NotificationSuppression.Detail))
                        {
                            if (!String.IsNullOrEmpty(item.Args.Detail) || !String.IsNullOrEmpty(item.Args.DetailSsml))
                            {
                                audioRequestTasks.Add(string.IsNullOrWhiteSpace(item.Args.DetailSsml)
                                    ? RetrieveAudioToFile(item, item.Args.Detail)
                                    : RetrieveAudioSsmlToFile(item, item.Args.DetailSsml));
                            }
                        }

                        await PlayAudioRequestsSequentially(audioRequestTasks);
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogError(ex, $"Failed to fetch/play notification: {item?.Args.Title} - {item?.Args.Detail}");
                    }
                }
            }
        }

        private async Task<string> RetrieveAudioToFile(HeraldQueueItem item, string text)
        {
            return await RetrieveAudioSsmlToFile(item, $"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"\">{System.Security.SecurityElement.Escape(text)}</voice></speak>");
        }

        private async Task<string> RetrieveAudioSsmlToFile(HeraldQueueItem item, string ssml)
        {
            return await speechManager.GetAudioFileFromSsml(ssml, item.SelectedVoice, item.SelectedStyle, item.SelectedRate);
        }

        private async Task PlayAudioRequestsSequentially(List<Task<string>> requestTasks)
        {
            foreach (var request in requestTasks)
            {
                string file = await request;
                if (!string.IsNullOrWhiteSpace(file))
                {
                    try
                    {
                        ErrorLogger.LogDebug($"Playing audio file: {file}");
                        await audioPlayer.Play(file);
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogError(ex, $"Failed to play {file}: {ex.Message}");
                    }

                    while (audioPlayer.Playing)
                        await Task.Delay(50);

                    // Explicit stop to ensure device is ready for next file.
                    // ...hopefully.
                    await audioPlayer.Stop(true);
                }
            }
            speechManager.CommitCache();
        }
    }
}
