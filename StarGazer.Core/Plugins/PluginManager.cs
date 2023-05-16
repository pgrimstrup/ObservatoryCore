using System.Data;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Observatory.Framework;
using Observatory.Framework.Interfaces;
using StarGazer.Framework.Interfaces;

namespace StarGazer.Plugins
{
    public class PluginManager
    {
        readonly IStarGazerCore _core;
        readonly ILogger _logger;
        readonly IAppSettings _settings;

        public Dictionary<string, PluginLoadState> Plugins { get; } = new();

        public IEnumerable<IObservatoryPlugin> ActivePlugins
        {
            get => Plugins.Values.Where(p => p.Instance != null && p.Error == null).Select(p => p.Instance);
        }

        public string PluginsFolder => Path.Combine(_core.CoreFolder, "Plugins");

        public PluginManager(IStarGazerCore core, ILogger<PluginManager> logger, IAppSettings settings)
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
                    var attrib = property.GetCustomAttribute<SettingDisplayName>();
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

        public void Shutdown()
        {
            foreach (var plugin in Plugins.Values.Where(p => p.Instance != null))
            {
                (plugin.Instance as IDisposable).Dispose();
                _core.SavePluginSettings(plugin.Instance);
                plugin.Instance = null;
            }
        }

        public void LoadPlugins()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            // Load inbuilt plugins first
            Type pluginType = typeof(IObservatoryPlugin);
            foreach (var type in Assembly.GetEntryAssembly().GetTypes())
            {
                if (pluginType.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                {
                    var pluginState = LoadPlugin(type);
                    Plugins[pluginState.SettingKey] = pluginState;
                    Debug.WriteLine($"Plugin {pluginState.SettingKey} loaded");
                }
            }

            // Load plugins listed in the app.config
            var solutionPlugins = _core.Services.GetRequiredService<IDebugPlugins>();
            if (solutionPlugins != null)
            {
                foreach (var name in solutionPlugins.PluginTypes.Keys)
                {
                    var type = Type.GetType(solutionPlugins.PluginTypes[name], false);
                    if (type == null)
                    {
                        Debug.WriteLine($"Plugin {name} ({solutionPlugins.PluginTypes[name]}) not found");
                    }
                    else
                    {
                        var pluginState = LoadPlugin(type);
                        Plugins[pluginState.SettingKey] = pluginState;
                        Debug.WriteLine($"Plugin {pluginState.SettingKey} loaded");
                    }
                }
            }

            // Load other plugins from the Documents folder
            ExtractPlugins(PluginsFolder);

            foreach (string file in Directory.GetFiles(PluginsFolder, "*.dll"))
            {
                try
                {
                    var assembly = Assembly.LoadFile(file);
                    foreach (var type in assembly.GetTypes())
                    {
                        if (pluginType.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                        {
                            var pluginState = LoadPlugin(type);
                            Plugins[pluginState.SettingKey] = pluginState;
                            Debug.WriteLine($"Plugin {pluginState.SettingKey} loaded");
                        }
                    }
                }
                catch (Exception ex)
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

            if (File.Exists(Path.Combine(PluginsFolder, name)))
                return Assembly.LoadFile(Path.Combine(PluginsFolder, name));

            if (File.Exists(Path.Combine(PluginsFolder, "deps", name)))
                return Assembly.LoadFile(Path.Combine(PluginsFolder, "deps", name));

            return null;
        }

        private PluginLoadState LoadPlugin(Type type)
        {
            var pluginState = new PluginLoadState();
            pluginState.SettingKey = type.FullName;
            pluginState.TypeName = type?.AssemblyQualifiedName;

            try
            {
                if (type != null)
                {
                    pluginState.Instance = Activator.CreateInstance(type) as IObservatoryPlugin;
                    if (pluginState.Instance == null)
                        throw new InvalidCastException("Created instance does not implement IObservatoryPlugin");

                    _core.LoadPluginSettings(pluginState.Instance);
                    pluginState.Instance.Load(_core);
                    _core.SavePluginSettings(pluginState.Instance);
                }
            }
            catch (Exception ex)
            {
                pluginState.Error = ex;
                _logger.LogError(ex, $"Loading Plugin {pluginState.SettingKey} ({pluginState.TypeName ?? "null"}) threw an exception");
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
