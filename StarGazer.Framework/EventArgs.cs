using System;

namespace StarGazer.Framework
{
    /// <summary>
    /// Extends the class used in the Observatory Framework to include per-notification
    /// voice control, and a cancellation flag. The passed through to the Herald will queue
    /// this instance, and will check the IsCancelled flag to determine whether to remove from the queue
    /// </summary>
    public class VoiceNotificationArgs : Observatory.Framework.NotificationArgs
    {
        public Guid Id;
        public bool IsCancelled;

        public string VoiceName;
        public string VoiceStyle;

        // 0 to 100, default is 50
        public int? VoiceRate;
        // 0 to 100, default is 50
        public int? VoicePitch;
        // 0 to 100, default is 75
        public int? VoiceVolume;

        public VoiceNotificationArgs()
        {
            Rendering = Observatory.Framework.NotificationRendering.NativeVocal;
        }
    }

    public class VisualNotificationArgs : Observatory.Framework.NotificationArgs
    {
        public Guid Id;
        public bool IsCancelled;

        public VisualNotificationArgs()
        {
            Rendering = Observatory.Framework.NotificationRendering.NativeVisual;
        }
    }
}
