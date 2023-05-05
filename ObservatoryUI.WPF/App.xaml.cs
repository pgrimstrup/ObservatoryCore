using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Observatory.Framework.Interfaces;
using Observatory.Plugins;
using Observatory;
using ObservatoryUI.WPF.Services;
using Syncfusion.SfSkinManager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using ObservatoryUI.WPF.Views;
using System.Net.Http;

namespace ObservatoryUI.WPF
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
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Mgo+DSMBaFt+QHFqUUdrXVNbdV5dVGpAd0N3RGlcdlR1fUUmHVdTRHRcQllhQH5WdExnW3ZZcHU=;Mgo+DSMBPh8sVXJ1S0d+WFBPd11dXmJWd1p/THNYflR1fV9DaUwxOX1dQl9gSXpRf0VmW3Zacn1dQGE=;ORg4AjUWIQA/Gnt2VFhhQlVFfV5AQmBIYVp/TGpJfl96cVxMZVVBJAtUQF1hSn5XdExjXH1XdHZcT2ld;MTg3NDYwOUAzMjMxMmUzMTJlMzQzMU0xQVNLYlRDaG9WWXY2MXBJRHVUM0E0SGVlRHJnekFBYXU2dytqMER3dTg9;MTg3NDYxMEAzMjMxMmUzMTJlMzQzMUpKaGh1N2s5MlpkZHVrU1NKSUJSazkxZ3g5b0c3LzFMMVEralpMd3EvSUE9;NRAiBiAaIQQuGjN/V0d+XU9Ad1RDX3xKf0x/TGpQb19xflBPallYVBYiSV9jS31TckdqWXpceXBWRmZVUA==;MTg3NDYxMkAzMjMxMmUzMTJlMzQzMUpRL0t1RElqblpObXlFU0lQRkFhMSt5Y29lVkkvWW1DNnZCK3I5M3FCQWc9;MTg3NDYxM0AzMjMxMmUzMTJlMzQzMUc2MnluSklMY05keERWemVpczRpNEptZTlGMWlQZFBacGE5WFpUeTl0Tk09;Mgo+DSMBMAY9C3t2VFhhQlVFfV5AQmBIYVp/TGpJfl96cVxMZVVBJAtUQF1hSn5XdExjXH1XdHdUQ2Ja;MTg3NDYxNUAzMjMxMmUzMTJlMzQzMUkzekxRS0xyVTNWOUdQcEphT1RCaHpFM29ZUjJzVzRTbjJ4SDBqeS9HYkE9;MTg3NDYxNkAzMjMxMmUzMTJlMzQzMUl1SGgxcUFRa3RoU1NaaEw5NmNLd0xhajJibU1NdjNjY25hem9Td1FRcVk9;MTg3NDYxN0AzMjMxMmUzMTJlMzQzMUpRL0t1RElqblpObXlFU0lQRkFhMSt5Y29lVkkvWW1DNnZCK3I5M3FCQWc9");

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
                logging.AddProvider(new FileLoggerProvider());
#if DEBUG
                logging.AddProvider(new DebugLoggerProvider());
#endif
            });

            // Register pages
            builder.AddSingleton<MainWindow>();
            builder.AddTransient<PluginView>();
            builder.AddTransient<SettingsWindow>();

            // Register services
            builder.AddSingleton<IObservatoryCoreAsync, ObservatoryCore>();
            builder.AddSingleton<IObservatoryCore>(services => services.GetRequiredService<IObservatoryCoreAsync>());
            builder.AddSingleton<IVoiceNotificationQueue, VoiceNotificationQueue>();
            builder.AddSingleton<IVisualNotificationQueue, VisualNotificationQueue>();

            builder.AddSingleton<PluginManager>();
            builder.AddSingleton<ILogMonitor, LogMonitor>();
            builder.AddSingleton<IAppSettings, AppSettings>();
            builder.AddSingleton<HttpClient>();

            builder.AddTransient<IDebugPlugins, DebugPlugins>();
            builder.AddTransient<IMainFormDispatcher, AppDispatcher>();

        }
    }
}