using System.Drawing;
using System.Text.Json;
using Observatory;
using Observatory.Framework.Interfaces;

namespace ObservatoryUI.Inbuilt
{
    public class AppSettings : IAppSettings
    {
        public string JournalFolder
        {
            get => Preferences.Default.Get(nameof(JournalFolder), "");
            set => Preferences.Default.Set(nameof(JournalFolder), value);
        }

        public bool AllowUnsigned
        {
            get => Preferences.Default.Get(nameof(AllowUnsigned), true);
            set => Preferences.Default.Set(nameof(AllowUnsigned), value);
        }
        public string CoreVersion
        {
            get => typeof(ObservatoryCore).Assembly.GetName().Version.ToString();
        }
        public WindowBounds MainWindowBounds
        {
            get => ParseBounds(Preferences.Default.Get(nameof(MainWindowBounds), ""));
            set => Preferences.Default.Set(nameof(MainWindowBounds), BoundsToString(value));
        }
        public bool StartMonitor
        {
            get => Preferences.Default.Get(nameof(StartMonitor), true);
            set => Preferences.Default.Set(nameof(StartMonitor), value);
        }
        public string ExportFolder
        {
            get => Preferences.Default.Get(nameof(ExportFolder), "");
            set => Preferences.Default.Set(nameof(ExportFolder), value);
        }
        public bool StartReadAll
        {
            get => Preferences.Default.Get(nameof(StartReadAll), false);
            set => Preferences.Default.Set(nameof(StartReadAll), value);
        }
        public string ExportStyle
        {
            get => Preferences.Default.Get(nameof(ExportStyle), "TAB");
            set => Preferences.Default.Set(nameof(ExportStyle), value);
        }

        public bool TryPrimeSystemContextOnStartMonitor
        {
            get => Preferences.Default.Get(nameof(TryPrimeSystemContextOnStartMonitor), true);
            set => Preferences.Default.Set(nameof(TryPrimeSystemContextOnStartMonitor), value);
        }

        public void LoadPluginSettings(IObservatoryPlugin plugin)
        {
            var text = Preferences.Default.Get("Plugin:" + plugin.Name, "");
            if (String.IsNullOrEmpty(text))
                return;

            plugin.Settings = JsonSerializer.Deserialize(text, plugin.Settings.GetType());
        }

        public void SavePluginSettings(IObservatoryPlugin plugin)
        {
            var text = JsonSerializer.Serialize(plugin.Settings);
            Preferences.Default.Set("Plugin:" + plugin.Name, text);
        }

        public void SaveSettings()
        {

        }

        WindowBounds ParseBounds(string text)
        {
            if (String.IsNullOrEmpty(text))
                return WindowBounds.Empty;

            var s = text.Split(',',';');
            if (s.Length != 4)
                return WindowBounds.Empty;

            if (Int32.TryParse(s[0], out int x) && Int32.TryParse(s[1], out int y) && Int32.TryParse(s[2], out int w) && Int32.TryParse(s[3], out int h))
                return new WindowBounds(x, y, w, h, 0);

            return WindowBounds.Empty;
        }

        string BoundsToString(WindowBounds r)
        {
            return $"{r.X},{r.Y},{r.Width},{r.Height}";
        }
    }
}
