using Microsoft.Extensions.Logging;
using Observatory;
using Observatory.Framework.Interfaces;
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

            builder.Logging.AddProvider(new FileLoggerProvider());
#if DEBUG
            builder.Logging.AddDebug();
#endif


            builder.Services.AddSingleton<IObservatoryCore, IObservatoryCore>();
            builder.Services.AddSingleton<ILogMonitor, LogMonitor>();

            return builder.Build();
        }
    }
}