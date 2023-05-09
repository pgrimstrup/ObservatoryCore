using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Enumeration;
using Microsoft.Extensions.Logging;
using NetCoreAudio;
using Observatory.Framework;
using Observatory.Framework.Interfaces;

namespace Observatory.Herald
{
    class HeraldQueue
    {
        private BlockingCollection<NotificationArgs> notifications;
        private SpeechRequestManager speechManager;
        //private Player audioPlayer;
        private ILogger ErrorLogger;
        private IAudioPlayback audioPlayer;
        private CancellationTokenSource CancellationToken;

        public HeraldQueue(SpeechRequestManager speechManager, ILogger errorLogger, IAudioPlayback playback)
        {
            this.speechManager = speechManager;
            notifications = new();
            audioPlayer = playback;
            ErrorLogger = errorLogger;
            CancellationToken = new CancellationTokenSource();

            // Fire and Forget the queue processing task
            Task.Run(ProcessQueueAsync);
        }

        internal void Enqueue(NotificationArgs notification)
        {
            // Volume is perceived logarithmically, convert to exponential curve
            // to make perceived volume more in line with value set.
            //item.Volume = ((byte)Math.Floor(Math.Pow(item.Volume / 100.0, 2.0) * 100));

            // Validate notification - all fields must be filled
            if (String.IsNullOrWhiteSpace(notification.VoiceName))
                throw new ArgumentException(nameof(notification.VoiceName));
            if (String.IsNullOrWhiteSpace(notification.VoiceRate))
                throw new ArgumentException(nameof(notification.VoiceRate));
            if (notification.VoiceVolume == null)
                throw new ArgumentException(nameof(notification.VoiceVolume));
            if (String.IsNullOrWhiteSpace(notification.VoiceStyle))
                throw new ArgumentException(nameof(notification.VoiceStyle));

            if (!String.IsNullOrEmpty(notification.TitleSsml) && !notification.TitleSsml.StartsWith("<speak>"))
                throw new ArgumentException(nameof(notification.TitleSsml));
            if (!String.IsNullOrEmpty(notification.DetailSsml) && !notification.DetailSsml.StartsWith("<speak>"))
                throw new ArgumentException(nameof(notification.DetailSsml));

            if (String.IsNullOrWhiteSpace(notification.AudioEncoding))
                throw new ArgumentException(nameof(notification.AudioEncoding));

            notifications.Add(notification);
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
                        await audioPlayer.SetVolume(item.VoiceVolume.GetValueOrDefault(75));
                        ErrorLogger.LogDebug("Processing notification: {0} - {1}", item.Title, item.Detail);

                        if (!String.IsNullOrWhiteSpace(item.Title))
                        {
                            if (lastTitle != null && item.Title == lastTitle && DateTime.Now.Subtract(lastTitleTime).TotalSeconds < 30)
                                item.Suppression |= NotificationSuppression.Title;

                            lastTitle = item.Title;
                            lastTitleTime = DateTime.Now;
                        }

                        if (!item.Suppression.HasFlag(NotificationSuppression.Title))
                        {
                            var fileInfo = await TextToSpeechTitle(item);
                            await PlayAudioFileAsync(fileInfo);
                        }

                        if (!item.Suppression.HasFlag(NotificationSuppression.Detail))
                        {
                            var fileInfo = await TextToSpeechDetail(item);
                            await PlayAudioFileAsync(fileInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogError(ex, $"Failed to fetch/play notification: {item?.Title} - {item?.Detail}");
                    }
                }
            }
        }

        private async Task<FileInfo> TextToSpeechTitle(NotificationArgs args)
        {
            if (!String.IsNullOrWhiteSpace(args.TitleSsml))
                return await speechManager.GetAudioFileFromSsmlAsync(args, args.TitleSsml);

            if (!String.IsNullOrWhiteSpace(args.Title))
                return await speechManager.GetAudioFileFromSsmlAsync(args, args.Title);

            return null;
        }

        private async Task<FileInfo> TextToSpeechDetail(NotificationArgs args)
        {
            if (!String.IsNullOrWhiteSpace(args.DetailSsml))
                return await speechManager.GetAudioFileFromSsmlAsync(args, args.DetailSsml);

            if (!String.IsNullOrWhiteSpace(args.Detail))
                return await speechManager.GetAudioFileFromSsmlAsync(args, args.Detail);

            return null;
        }

        private async Task PlayAudioFileAsync(FileInfo fileInfo)
        {
            if (fileInfo != null && fileInfo.Exists)
            {
                ErrorLogger.LogDebug($"Playing audio file: {fileInfo.FullName}");

                try
                {
                    await audioPlayer.PlayAsync(fileInfo.FullName);

                    while (audioPlayer.IsPlaying)
                        await Task.Delay(100);

                    await audioPlayer.StopAsync();
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogError(ex, $"Failed to play {fileInfo.FullName}: {ex.Message}");
                    await audioPlayer.StopAsync();
                }
            }
            speechManager.CommitCache();
        }
    }
}
