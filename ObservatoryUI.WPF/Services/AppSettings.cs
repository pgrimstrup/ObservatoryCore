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

namespace ObservatoryUI.WPF.Services
{
    public class AppSettingsData
    {
        public string AppTheme { get; set; } = "FluentDark";
        public string JournalFolder { get; set; } = "";

        public bool AllowUnsigned { get; set; } = true;

        public string CoreVersion
        {
            get => typeof(ObservatoryCore).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        }

        public WindowBounds MainWindowBounds { get; set; } = new WindowBounds();

        public bool StartMonitor { get; set; } = true;

        public string ExportFolder { get; set; } = "";

        public bool StartReadAll { get; set; } 

        public string ExportStyle { get; set; } = "";

        public bool TryPrimeSystemContextOnStartMonitor { get; set; } = true;

        public Dictionary<string, object> PluginSettings { get; set; } = new Dictionary<string, object>();

        public AppSettingsData()
        {
        }
    }

    public class AppSettings : AppSettingsData, IAppSettings
    {
        static Dictionary<string, PropertyInfo> Properties;
        static string FileName = "user.settings";
        static JsonSerializerOptions SerializerOptions;

        static AppSettings()
        {
            Properties = typeof(AppSettings).GetProperties().Where(p => p.CanRead && p.CanWrite).ToDictionary(p => p.Name);
            SerializerOptions = new JsonSerializerOptions();
            SerializerOptions.WriteIndented = true;
            SerializerOptions.IgnoreReadOnlyProperties = true;
            SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never;
            SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        }

        public AppSettings()
        {
            LoadSettings();
        }


        public void LoadSettings()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Elite Observatory", FileName);
            if (File.Exists(path))
            {
                try
                {
                    // Load the user settings and copy all properties across the this instance
                    var json = File.ReadAllText(path);
                    AppSettingsData? data = JsonSerializer.Deserialize<AppSettingsData>(json, SerializerOptions);
                    foreach(var prop in Properties.Values)
                    {
                        prop.SetValue(this, prop.GetValue(data));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        public void SaveSettings()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Elite Observatory", FileName);
            string json = JsonSerializer.Serialize(this, SerializerOptions);
            File.WriteAllText(path, json);
        }

        public void LoadPluginSettings(IObservatoryPlugin plugin)
        {
            string key = plugin.GetType().FullName!;

            // Convert the settings back to JSON so we can deserialize as the correct object type
            if (PluginSettings.TryGetValue(key, out var settings) && settings != null && plugin.Settings != null)
            {
                string json = JsonSerializer.Serialize(settings, SerializerOptions);
                var instance = JsonSerializer.Deserialize(json, plugin.Settings.GetType(), SerializerOptions);
                plugin.Settings = instance;
            }
        }

        public void SavePluginSettings(IObservatoryPlugin plugin)
        {
            if (plugin.Settings != null)
            {
                string key = plugin.GetType().FullName!;
                PluginSettings[key] = plugin.Settings;
                SaveSettings();
            }
        }
    }
}
