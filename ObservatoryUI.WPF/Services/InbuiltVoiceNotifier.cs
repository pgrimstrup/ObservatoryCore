﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework;
using Observatory.Framework.Interfaces;

namespace Observatory.Core.Services
{
    internal class VoiceNotifierSettings
    {

    }

    internal class InbuiltVoiceNotifier : IInbuiltNotifierAsync
    {
        public string Name => "Inbuilt Voice Notifier";

        public string ShortName => "Voice Notifier";

        public string Version => GetType().Assembly.GetName().Version!.ToString();

        public PluginUI PluginUI => null!;

        public object Settings { get; set; } = new VoiceNotifierSettings();

        public NotificationRendering Filter { get; } = NotificationRendering.NativeVisual;

        public void Load(IObservatoryCore observatoryCore)
        {

        }

        public void Unload()
        {

        }

        public void OnNotificationEvent(NotificationArgs notificationEventArgs)
        {

        }

        public void OnNotificationCancelled(Guid id)
        {

        }

        public Task OnNotificationEventAsync(Guid id, NotificationArgs notificationEventArgs)
        {
            throw new NotImplementedException();
        }

        public Task OnNotificationCancelledAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task OnNotificationEventAsync(NotificationArgs notificationEventArgs)
        {
            throw new NotImplementedException();
        }

        public Task LoadAsync(IObservatoryCoreAsync observatoryCore)
        {
            throw new NotImplementedException();
        }

        public Task UnloadAsync()
        {
            throw new NotImplementedException();
        }
    }
}