using Microsoft.Maui.Controls;
using Observatory.Framework.Files.Journal;
using Observatory.Framework.Interfaces;
using ObservatoryUI.Inbuilt;

namespace ObservatoryUI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window =  base.CreateWindow(activationState);
            window.Destroying += Window_Destroying;

            AppSettings settings = new AppSettings();
            var bounds = settings.MainWindowBounds;
            if (!bounds.IsEmpty)
            {
                window.X = bounds.X;
                window.Y = bounds.Y;
                window.Width = bounds.Width;
                window.Height = bounds.Height;
            }

            return window;
        }

        private void Window_Destroying(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            AppSettings settings = new AppSettings();
            settings.MainWindowBounds = new WindowBounds((int)(window.X * window.DisplayDensity), (int)(window.Y * window.DisplayDensity), (int)window.Width, (int)window.Height);
            base.CloseWindow(window);
        }
    }
}