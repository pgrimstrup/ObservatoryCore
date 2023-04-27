using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Data;
using Observatory.Framework.Interfaces;
using System.IO;
using Observatory.Framework;
using System.Text.Json;
using System.Configuration;

namespace Observatory.PluginManagement
{
    public class PluginManager
    {
        public readonly List<DataTable> pluginTables;
        private readonly ObservatoryCore _core;

        public List<PluginLoadState> Plugins { get; } = new();

        internal PluginManager(ObservatoryCore core)
        {
            _core = core;
            pluginTables = new();
        }

        public static Dictionary<PropertyInfo, string> GetSettingDisplayNames(object settings)
        {
            var settingNames = new Dictionary<PropertyInfo, string>();

            if (settings != null)
            {
                var properties = settings.GetType().GetProperties();
                foreach (var property in properties)
                {
                    var ignore = property.GetCustomAttribute<SettingIgnoreAttribute>();
                    if (ignore != null)
                        continue;

                    var attrib = property.GetCustomAttribute<SettingDisplayNameAttribute>();
                    if (attrib == null)
                    {
                        settingNames.Add(property, property.Name);
                    }
                    else
                    {
                        settingNames.Add(property, attrib.DisplayName);
                    }
                }
            }
            return settingNames;
        }

        public void SaveSettings(IObservatoryPlugin plugin, object settings)
        {
            string savedSettings = Properties.Core.Default.PluginSettings;
            Dictionary<string, object> pluginSettings;

            if (!String.IsNullOrWhiteSpace(savedSettings))
            {
                pluginSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(savedSettings);
            }
            else
            {
                pluginSettings = new();
            }

            if (pluginSettings.ContainsKey(plugin.Name))
            {
                pluginSettings[plugin.Name] = settings;
            }
            else
            {
                pluginSettings.Add(plugin.Name, settings);
            }

            string newSettings = JsonSerializer.Serialize(pluginSettings, new JsonSerializerOptions()
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
            });

            Properties.Core.Default.PluginSettings = newSettings;
            Properties.Core.Default.Save();
        }

        public void Shutdown()
        {
            foreach(var plugin in Plugins.Where(p => p.Instance != null))
            {
                plugin.Instance.Unload();
            }
        }

        public void LoadPlugins()
        {
            foreach(var key in ConfigurationManager.AppSettings.AllKeys)
            {
                if (key.StartsWith("Plugin:"))
                {
                    var pluginState = new PluginLoadState();
                    pluginState.SettingKey = key;
                    pluginState.TypeName = ConfigurationManager.AppSettings[key];

                    try
                    {
                        pluginState.Instance = Activator.CreateInstance(Type.GetType(pluginState.TypeName)) as IObservatoryPlugin;
                        if (pluginState.Instance == null)
                            throw new InvalidCastException("Created instance does not implement IObservatoryPlugin");
                    }
                    catch(Exception ex)
                    {
                        pluginState.Error = ex;
                    }

                    Plugins.Add(pluginState);
                }
            }
        }

        public void LoadPluginSettings()
        {
            foreach (var plugin in Plugins.Where(p => p.Instance != null && p.Error == null))
            {
                try
                {
                    var settings = GetSettings(plugin.Instance);
                    if (settings != null)
                        plugin.Instance.Settings = settings;
                    plugin.Instance.Load(_core);
                }
                catch (Exception ex)
                {
                    // Bad plugin
                    plugin.Error = ex;
                }
            }
        }

        private object GetSettings(IObservatoryPlugin plugin)
        {
            string savedSettings = Properties.Core.Default.PluginSettings;
            Dictionary<string, object> pluginSettings;

            if (!String.IsNullOrWhiteSpace(savedSettings))
            {
                pluginSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(savedSettings);
            }
            else
            {
                pluginSettings = new();
            }

            if (pluginSettings.ContainsKey(plugin.Name))
            {
                var settingsElement = (JsonElement)pluginSettings[plugin.Name];
                var settingsObject = JsonSerializer.Deserialize(settingsElement.GetRawText(), plugin.Settings.GetType());
                return settingsObject;
            }

            return null;
        }


        private static void ExtractPlugins(string pluginFolder)
        {
            var files = Directory.GetFiles(pluginFolder, "*.zip")
                .Concat(Directory.GetFiles(pluginFolder, "*.eop")); // Elite Observatory Plugin

            foreach (var file in files)
            {
                try
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(file, pluginFolder, true);
                    File.Delete(file);
                }
                catch 
                { 
                    // Just ignore files that don't extract successfully.
                }
            }
        }
    }

}
