using System.ComponentModel;
using System.Windows;
using Observatory.Framework.Interfaces;
using Syncfusion.SfSkinManager;
using Syncfusion.Windows.Shared;

namespace ObservatoryUI.WPF
{
    /// <summary>
    /// Interaction logic for ChromelessWindow1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly IObservatoryCoreAsync _core;
        readonly IAppSettings _settings;

        public MainWindow(IObservatoryCoreAsync core, IAppSettings settings)
        {
            SfSkinManager.SetTheme(this, new Theme("FluentDark"));
            InitializeComponent();

            _core = core;
            _settings = settings;

            if (!_settings.MainWindowBounds.IsEmpty)
            {
                this.Left = _settings.MainWindowBounds.X;
                this.Top = _settings.MainWindowBounds.Y;
                this.Width = _settings.MainWindowBounds.Width;
                this.Height = _settings.MainWindowBounds.Height;
                this.WindowState = (WindowState)_settings.MainWindowBounds.State;
            }

            var plugins = _core.Initialize();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
                _settings.MainWindowBounds = new WindowBounds((int)Left, (int)Top, (int)Width, (int)Height, (int)WindowState);
            else
                _settings.MainWindowBounds = new WindowBounds((int)RestoreBounds.Left, (int)RestoreBounds.Top, (int)RestoreBounds.Width, (int)RestoreBounds.Height, (int)WindowState);

            _settings.SaveSettings();
        }

    }
}
