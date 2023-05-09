using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Observatory.Framework.Interfaces;
using Observatory;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Observatory.Framework;
using System.Text.Json.Serialization;

namespace ObservatoryUI.WPF.Services
{
    public class AppSettings: IAppSettings
    {
        public string AppTheme { get; set; } = "FluentDark";
        public string JournalFolder { get; set; } = "";

        public bool AllowUnsigned { get; set; } = true;

        public WindowBounds MainWindowBounds { get; set; } = new WindowBounds();

        public bool StartMonitor { get; set; } = true;

        public string ExportFolder { get; set; } = "";

        public bool StartReadAll { get; set; } 

        public string ExportStyle { get; set; } = "";

        public bool InbuiltVoiceEnabled { get; set; } = true;
        public int VoiceVolume { get; set; } = 75;
        public string VoiceRate { get; set; }
        public string VoiceName { get; set; } = "";
        public string VoiceStyle { get; set; } = "";
        public string GoogleTextToSpeechApiKey { get; set; } = "";
        public string AzureTextToSpeechApiKey { get; set; } = "";
        public bool VoiceWelcomeMessage { get; set; } = true;

        public bool InbuiltPopupsEnabled {  get; set; } = true;

        [JsonIgnore]
        public string CoreVersion
        {
            get => typeof(ObservatoryCore).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        }

        [JsonIgnore]
        public string PluginStorageFolder
        {
            get => "";
        }

        public AppSettings()
        {
        }

    }

    //public class AppSettings : AppSettingsData
    //{
    //    static string FileName = "user.settings";


    //    public AppSettings()
    //    {
    //        LoadSettings();
    //    }


    //    public void LoadSettings()
    //    {
    //        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Elite Observatory", FileName);
    //        if (File.Exists(path))
    //        {
    //            try
    //            {
    //                // Load the user settings and copy all properties across the this instance.
    //                // Note that after loading, the PluginSettings contains a list of JsonElements
    //                var json = File.ReadAllText(path);
    //                AppSettingsData? data = JsonSerializer.Deserialize<AppSettingsData>(json, CoreExtensions.SerializerOptions);
    //                foreach(var prop in typeof(AppSettingsData).GetProperties())
    //                {
    //                    if(prop.CanRead && prop.CanWrite)
    //                        prop.SetValue(this, prop.GetValue(data));
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                Debug.WriteLine(ex.Message);
    //            }
    //        }
    //    }

    //    public void SaveSettings()
    //    {
    //        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Elite Observatory", FileName);
    //        string json = JsonSerializer.Serialize(this, CoreExtensions.SerializerOptions);
    //        File.WriteAllText(path, json);
    //    }

    //    public void GetPluginSettings(IObservatoryPlugin plugin)
    //    {
    //        string key = plugin.GetType().FullName!;

    //        if (PluginSettings.TryGetValue(key, out var settings) && settings != null && plugin.Settings != null)
    //        {
    //            if (settings.GetType() == plugin.Settings.GetType())
    //                // Settings are already stored as the correct type, so can be assigned directly
    //                plugin.Settings = settings;
    //            else
    //            {
    //                // Convert the settings to the correct object type
    //                settings = settings.CopyAs(plugin.Settings.GetType());
    //                PluginSettings[key] = settings;
    //                plugin.Settings = settings;
    //            }
    //        }
    //    }

    //    public void AddPluginSettings(IObservatoryPlugin plugin)
    //    {
    //        if (plugin.Settings != null)
    //        {
    //            string key = plugin.GetType().FullName!;
    //            PluginSettings[key] = plugin.Settings;
    //        }
    //    }
    //}
}
