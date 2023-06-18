using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Enumeration;
using Microsoft.Extensions.Logging;
using NetCoreAudio;
using Observatory.Framework;
using StarGazer.Framework;
using StarGazer.Framework.Interfaces;

namespace StarGazer.Herald
{
    class HeraldQueue
    {
        private BlockingCollection<VoiceNotificationArgs> textToSpeech;
        private BlockingCollection<VoiceNotificationArgs> notifications;
        private SpeechRequestManager speechManager;
        //private Player audioPlayer;
        private ILogger ErrorLogger;
        private IAudioPlayback audioPlayer;
        private CancellationTokenSource CancellationToken;
        private ManualResetEvent continueProcessing = new ManualResetEvent(true);

        private Task _textToSpeechTask;
        private Task _notificationTask;

        public HeraldQueue(SpeechRequestManager speechManager, ILogger errorLogger, IAudioPlayback playback)
        {
            this.speechManager = speechManager;
            notifications = new BlockingCollection<VoiceNotificationArgs>();
            textToSpeech = new BlockingCollection<VoiceNotificationArgs>();
            audioPlayer = playback;
            ErrorLogger = errorLogger;
            CancellationToken = new CancellationTokenSource();

            // Fire and Forget the queue processing tasks
            _notificationTask = Task.Run(ProcessNotifications);
            _textToSpeechTask = Task.Run(ProcessTextToSpeech);
        }

        internal void Enqueue(VoiceNotificationArgs notification)
        {
            // Volume is perceived logarithmically, convert to exponential curve
            // to make perceived volume more in line with value set.
            //item.Volume = ((byte)Math.Floor(Math.Pow(item.Volume / 100.0, 2.0) * 100));

            if (!String.IsNullOrEmpty(notification.TitleSsml) && !notification.TitleSsml.StartsWith("<speak"))
                throw new ArgumentException(nameof(notification.TitleSsml));
            if (!String.IsNullOrEmpty(notification.DetailSsml) && !notification.DetailSsml.StartsWith("<speak"))
                throw new ArgumentException(nameof(notification.DetailSsml));

            // Validate notification - all fields must be filled
            if (String.IsNullOrWhiteSpace(notification.VoiceName))
                throw new ArgumentException(nameof(notification.VoiceName));
            if (notification.VoiceVolume == null)
                throw new ArgumentException(nameof(notification.VoiceVolume));
            if (String.IsNullOrWhiteSpace(notification.VoiceStyle))
                throw new ArgumentException(nameof(notification.VoiceStyle));

            textToSpeech.Add(notification);
        }

        public void Cancel()
        {
            CancellationToken.Cancel();
        }

        /// <summary>
        /// Notifications are added to the TextToSpeech queue first. The Title and Detail are 
        /// immediately downloaded so it is ready for playback when needed.
        /// Once the speech files have been downloaded, the item is added to the notifications
        /// queue which then plays the speech file.
        /// </summary>
        /// <returns></returns>
        private async Task ProcessTextToSpeech()
        {
            string lastTitle = null;
            DateTime lastTitleTime = DateTime.Now;
            CancellationToken cancelToken = CancellationToken.Token;
            while (!Environment.HasShutdownStarted && !cancelToken.IsCancellationRequested)
            {
                if (textToSpeech.TryTake(out var item, 100, cancelToken))
                {
                    try
                    {
                        if (item.IsCancelled)
                            continue;

                        // Determine whether the title should be played. 
                        if (!String.IsNullOrWhiteSpace(item.Title))
                        {
                            if (lastTitle != null && item.Title == lastTitle && DateTime.Now.Subtract(lastTitleTime).TotalSeconds < 30)
                                item.Suppression |= NotificationSuppression.Title;

                            lastTitle = item.Title;
                            lastTitleTime = DateTime.Now;
                        }

                        if (!item.Suppression.HasFlag(NotificationSuppression.Title))
                        {
                            // Download the speech file for the title if needed
                            await TextToSpeechTitle(item);
                        }

                        if (!item.Suppression.HasFlag(NotificationSuppression.Detail))
                        {
                            // Download the speech file for the detail if needed
                            await TextToSpeechDetail(item);
                        }

                        // Add to the notifications queue
                        notifications.Add(item);
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogError(ex, $"Failed to fetch/play notification: {item?.Title} - {item?.Detail}");
                    }
                }
            }
        }

        /// <summary>
        /// This handler simply plays the speech file for the Title and Detail. If the file doesn't exist due
        /// to a change in the text (via the Update method), then it will be downloaded.
        /// </summary>
        /// <returns></returns>
        private async Task ProcessNotifications()
        {
            CancellationToken cancelToken = CancellationToken.Token;
            while (!Environment.HasShutdownStarted && !cancelToken.IsCancellationRequested)
            {
                if (notifications.TryTake(out var item, 100, cancelToken))
                {
                    // Herald will wait for up to 5 seconds before processing this notification if needed by Update/Cancel
                    continueProcessing.WaitOne(TimeSpan.FromSeconds(5));
                    try
                    {
                        if (item.IsCancelled)
                            continue;

                        await audioPlayer.SetVolume(item.VoiceVolume.GetValueOrDefault(75));

                        if (!item.Suppression.HasFlag(NotificationSuppression.Title))
                        {
                            // Get the speech file (should already exist) and play
                            var fileInfo = await TextToSpeechTitle(item);
                            await PlayAudioFileAsync(fileInfo);
                        }

                        if (!item.Suppression.HasFlag(NotificationSuppression.Detail))
                        {
                            // Get the speech file (should already exist) and play
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

        private async Task<FileInfo> TextToSpeechTitle(VoiceNotificationArgs args)
        {
            if (!String.IsNullOrWhiteSpace(args.TitleSsml))
                return await speechManager.GetAudioFileFromSsmlAsync(args, args.TitleSsml);

            if (!String.IsNullOrWhiteSpace(args.Title))
                return await speechManager.GetAudioFileFromSsmlAsync(args, args.Title.Trim(' ', '.'));

            return null;
        }

        private async Task<FileInfo> TextToSpeechDetail(VoiceNotificationArgs args)
        {
            if (!String.IsNullOrWhiteSpace(args.DetailSsml))
                return await speechManager.GetAudioFileFromSsmlAsync(args, args.DetailSsml);

            if (!String.IsNullOrWhiteSpace(args.Detail))
                return await speechManager.GetAudioFileFromSsmlAsync(args, args.Detail.Trim(' ', '.'));

            return null;
        }

        private async Task PlayAudioFileAsync(FileInfo fileInfo)
        {
            if (fileInfo != null && fileInfo.Exists)
            {
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
        }

        internal void CancelNotification(Guid id)
        {
            continueProcessing.Reset();
            try
            {
                foreach (var item in notifications.Where(i => i.Id == id))
                {
                    item.IsCancelled = true;
                }
            }
            finally
            {
                continueProcessing.Set();
            }
        }

        internal void UpdateNotification(Guid id, VoiceNotificationArgs updated)
        {
            continueProcessing.Reset();
            try
            {
                foreach (var item in notifications.Where(i => i.Id == id))
                {
                    item.Title = updated.Title;
                    item.TitleSsml = updated.TitleSsml;
                    item.Detail = updated.Detail;
                    item.DetailSsml = updated.DetailSsml;
                    item.Rendering = updated.Rendering;
                    item.Suppression = updated.Suppression;
                    item.VoiceRate = updated.VoiceRate;
                    item.VoiceStyle = updated.VoiceStyle;
                    item.VoiceName = updated.VoiceName;
                    item.VoiceVolume = updated.VoiceVolume;
                    item.IsCancelled = updated.IsCancelled;
                }
            }
            finally
            {
                continueProcessing.Set();
            }
        }
    }
}
