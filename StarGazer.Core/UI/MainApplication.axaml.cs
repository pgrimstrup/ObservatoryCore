using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Observatory.PluginManagement;
using Observatory.UI.ViewModels;

namespace Observatory.UI
{
    public class MainApplication : Application
    {
        private PluginCore _core;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _core = new PluginCore();
                desktop.MainWindow = new Views.MainWindow()
                {
                    DataContext = new MainWindowViewModel(_core)
                };

                desktop.MainWindow.Closing += (o, e) =>
                {
                    _core.Shutdown();
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
