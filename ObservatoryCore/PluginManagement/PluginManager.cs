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
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Observatory.PluginManagement
{
    public class PluginManager
    {
        readonly IObservatoryCoreAsync _core;
        readonly ILogger _logger;
        readonly IAppSettings _settings;

        public Dictionary<string, PluginLoadState> Plugins { get; } = new();

        public IEnumerable<IObservatoryPlugin> ActivePlugins
        {
            get => Plugins.Values.Where(p => p.Instance != null && p.Error == null).Select(p => p.Instance);
        }


        public PluginManager(IObservatoryCoreAsync core, ILogger<PluginManager> logger, IAppSettings settings)
        {
            _core = core;
            _logger = logger;
            _settings = settings;   
        }

        public static Dictionary<PropertyInfo, string> GetSettingDisplayNames(object settings)
        {
            var settingNames = new Dictionary<PropertyInfo, string>();

            if (settings != null)
            {
                var properties = settings.GetType().GetProperties();
                foreach (var property in properties)
                {
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
            plugin.Settings = settings;
            _settings.SavePluginSettings(plugin);
        }

        public void Shutdown()
        {
            foreach(var plugin in Plugins.Values.Where(p => p.Instance != null))
            {
                (plugin.Instance as IDisposable).Dispose();
                plugin.Instance = null;
            }
        }

        public void LoadPlugins()
        {
            // Load inbuilt plugins first
            Type pluginType = typeof(IObservatoryPlugin);
            foreach(var type in Assembly.GetEntryAssembly().GetTypes())
            {
                if(pluginType.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                {
                    var pluginState = LoadPlugin("Inbuilt:" + type.Name, type);
                    Plugins[type.FullName] = pluginState;
                    Debug.WriteLine($"Plugin {pluginState.SettingKey} ({pluginState.TypeName}) loaded");
                }
            }

            // Load plugins listed in the app.config
            var solutionPlugins = _core.Services.GetRequiredService<IDebugPlugins>();
            if(solutionPlugins != null)
            {
                foreach(var key in solutionPlugins.PluginTypes.Keys)
                {
                    var type = Type.GetType(solutionPlugins.PluginTypes[key], false);
                    if (type == null)
                    {
                        Debug.WriteLine($"Plugin {key} ({solutionPlugins.PluginTypes[key]}) not found");
                    }
                    else
                    {
                        var pluginState = LoadPlugin("Solution:" + key, type);
                        Plugins[type.FullName] = pluginState;
                        Debug.WriteLine($"Plugin {pluginState.SettingKey} ({pluginState.TypeName}) loaded");
                    }
                }
            }

            // Load other plugins from the Documents folder
            var pluginsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ObservatoryCore", "Plugins");
            ExtractPlugins(pluginsFolder);

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            foreach(string file in Directory.GetFiles(pluginsFolder, "*.dll"))
            {
                try
                {
                    var assembly = Assembly.LoadFile(file);
                    foreach(var type in assembly.GetTypes())
                    {
                        if (pluginType.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                        {
                            var pluginState = LoadPlugin("Plugin:" + type.Name, type);
                            Plugins[type.FullName] = pluginState;
                            Debug.WriteLine($"Plugin {pluginState.SettingKey} ({pluginState.TypeName}) loaded");
                        }
                    }
                }
                catch(Exception ex)
                {
                    // Ignore any load errors
                    _logger.LogError(ex, $"Unable to load Plugin from file {file}");
                }
            }
        }

        // Called when an assembly can't be found in the GAC or the application folder, so we need to do a little search for it
        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = args.Name.Split(',').First() + ".dll";
            var pluginsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Elite Observatory", "Plugins");

            if(!File.Exists(Path.Combine(pluginsFolder, name)))
            {
                // Check the deps subfolder
                pluginsFolder = Path.Combine(pluginsFolder, "deps");
                if (!File.Exists(Path.Combine(pluginsFolder, name)))
                    return null;
            }

            return Assembly.LoadFile(Path.Combine(pluginsFolder, name));
        }

        private PluginLoadState LoadPlugin(string key, Type type)
        {
            var pluginState = new PluginLoadState();
            pluginState.SettingKey = key;
            pluginState.TypeName = type?.AssemblyQualifiedName;

            try
            {
                if (type != null)
                {
                    pluginState.Instance = Activator.CreateInstance(type) as IObservatoryPlugin;
                    if (pluginState.Instance == null)
                        throw new InvalidCastException("Created instance does not implement IObservatoryPlugin");

                    _settings.LoadPluginSettings(pluginState.Instance);
                    pluginState.Instance.Load(_core);
                    _settings.SavePluginSettings(pluginState.Instance);
                }
            }
            catch (Exception ex)
            {
                pluginState.Error = ex;
                _logger.LogError(ex, $"Loading Plugin {key} ({pluginState.TypeName ?? "null"}) threw an exception");
            }

            return pluginState;
        }

        private void ExtractPlugins(string pluginFolder)
        {
            if (!Directory.Exists(pluginFolder))
                Directory.CreateDirectory(pluginFolder);

            var files = Directory.GetFiles(pluginFolder, "*.zip")
                .Concat(Directory.GetFiles(pluginFolder, "*.eop")); 

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
