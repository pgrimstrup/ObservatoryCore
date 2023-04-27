using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework;
using Observatory.Framework.Interfaces;

namespace ObservatoryUI.Inbuilt
{
    internal class VoiceNotification : IObservatoryNotifier
    {
        public string Name => throw new NotImplementedException();

        public string Version => throw new NotImplementedException();

        public PluginUI PluginUI => throw new NotImplementedException();

        public object Settings { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public NotificationRendering Filter { get; } = NotificationRendering.NativeVocal;

        public void Load(IObservatoryCore observatoryCore)
        {
            throw new NotImplementedException();
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
