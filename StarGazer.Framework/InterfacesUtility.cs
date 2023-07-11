using Observatory.Framework;

namespace StarGazer.Framework.Interfaces
{
    public interface ICanRaisePropertyChanged
    {
        void RaisePropertyChanged(string propertyName);
    }
    public interface ILogMonitor
    {
        void Start();
        void Stop();
        void ReadAll();
        void ReadCurrent();

        Task ReadAllAsync();
        Task ReadCurrentAsync();

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
        public WindowBounds MainWindowBounds { get; set; }
        public bool StartMonitor { get; set; }
        public string ExportFolder { get; set; }
        public bool StartReadAll { get; set; }
        public string ExportStyle { get; set; }

        public bool InbuiltVoiceEnabled { get; set; }
        // Volume for inbuilt voice notifier: 0 to 100
        public int VoiceVolume { get; set; }
        // Rate for inbuilt voice notifier: 0 to 100, default 50
        public int VoiceRate { get; set; }
        public string VoiceName { get; set; }
        public string GoogleTextToSpeechApiKey { get; set; }
        public string AzureTextToSpeechApiKey { get; set; }
        public bool VoiceWelcomeMessage { get; set; }
        public bool InbuiltPopupsEnabled { get; set; }
        public Dictionary<string, double> GridFontSizes { get; set; }
        public string CoreVersion { get; }
    }

    public interface IVoiceNotificationQueue
    {
        void Add(VoiceNotificationArgs msg);
        void Update(VoiceNotificationArgs msg);
        void Cancel(Guid id);

        void Shutdown();
    }

    public interface IVisualNotificationQueue
    {
        void Add(VisualNotificationArgs msg);
        void Update(VisualNotificationArgs msg);
        void Cancel(Guid id);

        void Shutdown();
    }

    public interface IAudioPlayback
    {
        Task SetVolume(int volume);
        Task PlayAsync(string filename);
        Task StopAsync();
        bool IsPlaying { get; }
        string FileName { get; }

        string ConvertWavToOpus(string sourceFile);
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
