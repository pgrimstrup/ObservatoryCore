﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework;
using Observatory.Framework.Interfaces;

namespace ObservatoryUI.Inbuilt
{
    internal class PopupNotification : IObservatoryNotifier
    {
        public string Name => "Inbuilt Popup Notifier";

        public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public PluginUI PluginUI => null;

        public object Settings 
        {
            get => null;
            set { }
        }

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
    }
}