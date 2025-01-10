    using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using StarGazer.Framework.Interfaces;
using StarGazer.Plugins;
using StarGazer;
using StarGazer.UI.Services;
using Syncfusion.SfSkinManager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using StarGazer.UI.Views;
using System.Net.Http;
using System.IO;
using Observatory.Framework.Interfaces;

namespace StarGazer.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider _services;

        public IServiceProvider Services => _services;

        public App()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NMaF5cXmBCf0x3Q3xbf1x1ZFRGal5WTnZeUj0eQnxTdEBjWH9XcnZXQWBdUkF1Wg==");

            ServiceCollection builder = new ServiceCollection();
            ConfigureServices(builder);
            _services = builder.BuildServiceProvider();
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            var mainWindow = _services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(ServiceCollection builder)
        {
            builder.AddLogging(logging => {
#if DEBUG
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddDebugLogging();
#else
                logging.SetMinimumLevel(LogLevel.Information);
#endif
                logging.AddFileLogging(options => {
                    options.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Star Gazer", "Logs", "observatory.log");
                    options.RolloverCount = 10;
                    options.DailyRollover = true;
                });
            });

            // Register pages
            builder.AddSingleton<MainWindow>();
            builder.AddTransient<PluginView>();
            builder.AddTransient<SettingsWindow>();

            // Register services
            builder.AddSingleton<IStarGazerCore, StarGazerCore>();
            builder.AddSingleton<IObservatoryCore>(services => services.GetRequiredService<IStarGazerCore>());
            builder.AddSingleton<IVoiceNotificationQueue, InbuiltVoiceNotification>();
            builder.AddSingleton<IVisualNotificationQueue, VisualNotificationQueue>();
            builder.AddSingleton<IAudioPlayback, NAudioService>();

            builder.AddSingleton<PluginManager>();
            builder.AddSingleton<ILogMonitor, LogMonitor>();
            builder.AddSingleton<IAppSettings, AppSettings>();
            builder.AddSingleton<HttpClient>();

            builder.AddTransient<IDebugPlugins, DebugPlugins>();
            builder.AddTransient<IMainFormDispatcher, AppDispatcher>();

        }
    }
}