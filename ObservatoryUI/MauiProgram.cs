using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Observatory;
using Observatory.Framework.Interfaces;
using Observatory.PluginManagement;
using ObservatoryUI.Inbuilt;
using Syncfusion.Maui.Core.Hosting;

namespace ObservatoryUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts => {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.ConfigureSyncfusionCore();

            // Register loggers
            builder.Logging.AddProvider(new FileLoggerProvider());
#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Register pages
            builder.Services.AddSingleton<MainPage>();

            // Register services
            builder.Services.AddSingleton<IObservatoryCoreAsync, ObservatoryCore>();
            builder.Services.AddSingleton<IObservatoryCore>(services => services.GetService<IObservatoryCoreAsync>());

            builder.Services.AddSingleton<PluginManager>();
            builder.Services.AddSingleton<ILogMonitor, LogMonitor>();
            builder.Services.AddTransient<IDebugPlugins, SolutionPlugins>();
            builder.Services.AddTransient<IMainFormDispatcher, AppDispatcher>();
            builder.Services.AddTransient<IAppSettings, AppSettings>();

            return builder.Build();
        }
    }
}