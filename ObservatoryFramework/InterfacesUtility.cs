using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Observatory.Framework.Interfaces
{
    public interface ILogMonitor
    {
        void Start();
        void Stop();

        LogMonitorState CurrentState { get; }

        event EventHandler<LogMonitorStateChangedEventArgs> LogMonitorStateChanged;

        event EventHandler<JournalEventArgs> JournalEntry;

        event EventHandler<JournalEventArgs> StatusUpdate;
    }

    public interface IDebugPlugins
    {
        IDictionary<string, string> PluginTypes { get; }
    }

    public interface IMainFormDispatcher
    {
        void Run(Action action);
        Task RunAsync(Action action);
    }

    public interface IAppSettings
    {
        string AppTheme { get; set; }
        string JournalFolder { get; set; }
        bool AllowUnsigned { get; set; }
        public string CoreVersion { get;  }
        public WindowBounds MainWindowBounds { get; set; }
        public bool StartMonitor { get; set; }
        public string ExportFolder { get; set; }
        public bool StartReadAll { get; set; }
        public string ExportStyle { get; set; }

        public bool InbuiltVoiceEnabled { get; set; }
        public int VoiceVolume { get; set; }
        public int VoiceRate { get; set; }
        public string VoiceName { get; set; }
        public bool VoiceWelcomeMessage { get; set; }

        public bool InbuiltPopupsEnabled { get; set; }

        public void LoadPluginSettings(IObservatoryPlugin plugin);
        public void SavePluginSettings(IObservatoryPlugin plugin);

        public void SaveSettings();
    }

    public interface IVoiceNotificationQueue
    {
        void Add(VoiceMessage msg);
        void Update(VoiceMessage msg);
        void Cancel(Guid id);

        void Shutdown();
    }

    public interface IVisualNotificationQueue
    {
        void Add(PopupMessage msg);
        void Update(PopupMessage msg);
        void Cancel(Guid id);

        void Shutdown();
    }

    public class VoiceMessage
    {
        public Guid Id;
        public string Title;
        public string Detail;
        public string VoiceName;
        public int VoiceRate;
        public int VoiceVolume;
        public bool Cancelled;
    }

    public class PopupMessage
    {
        public Guid Id;
        public string Title;
        public string Detail;
        public int Timeout;
        public int X;
        public int Y;

        public DateTime? TimeShown;
        public DateTime? AutoCloseTime;
    }

    public class WindowBounds
    {
        public static readonly WindowBounds Empty = new WindowBounds();

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int State { get; set; }

        public bool IsEmpty => X == 0 && Y == 0 && Width == 0 && Height == 0;

        public WindowBounds()
        {

        }

        public WindowBounds(int x, int y, int width, int height, int state)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            State = state;
        }
    }
   
}
