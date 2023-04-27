using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Observatory;
using Observatory.Framework.Interfaces;
using Observatory.PluginManagement;
using ObservatoryUI.Inbuilt;

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

            // Register loggers
            builder.Logging.AddProvider(new FileLoggerProvider());
#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Register pages
            builder.Services.AddTransient<MainPage>();

            // Register services
            builder.Services.AddSingleton<IObservatoryCore, ObservatoryCore>();
            builder.Services.AddSingleton<ILogMonitor, LogMonitor>();

            builder.Services.AddSingleton<PluginManager>();
            builder.Services.AddTransient<ISolutionPlugins, SolutionPlugins>();

            return builder.Build();
        }
    }
}