using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Observatory.Framework;
using Observatory.Framework.Interfaces;
using ObservatoryUI.WPF.ViewModels;
using ObservatoryUI.WPF.Views;
using Syncfusion.SfSkinManager;
using Syncfusion.Windows.Tools.Controls;

namespace ObservatoryUI.WPF
{
    /// <summary>
    /// Interaction logic for ChromelessWindow1.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        readonly IObservatoryCoreAsync _core;
        readonly IAppSettings _settings;
        readonly string _dockStateFile;

        public ObservableCollection<PluginViewModel> PluginViews { get; } = new ObservableCollection<PluginViewModel>();

        public MainWindow(IObservatoryCoreAsync core, IAppSettings settings)
        {
            SfSkinManager.SetTheme(this, new Theme(settings.AppTheme));
            InitializeComponent();

            _core = core;
            _settings = settings;
            _dockStateFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Elite Observatory", "docking_state.xml");

            if (!_settings.MainWindowBounds.IsEmpty)
            {
                this.Left = _settings.MainWindowBounds.X;
                this.Top = _settings.MainWindowBounds.Y;
                this.Width = _settings.MainWindowBounds.Width;
                this.Height = _settings.MainWindowBounds.Height;
                this.WindowState = (WindowState)_settings.MainWindowBounds.State;
            }

            var plugins = _core.Initialize();
            foreach(var plugin in plugins.Where(p => p.PluginUI?.DataGrid != null))
            {
                var view = _core.Services.GetRequiredService<PluginView>();
                var viewModel = new PluginViewModel(plugin, view);

                PluginViews.Add(viewModel);
            }

            this.DataContext = this;
        }

        protected void OnLoaded(object sender,  RoutedEventArgs e)
        {
            Ribbon.BackStageButton.Visibility = Visibility.Collapsed;

            foreach (var plugin in PluginViews)
            {
                DocumentContainer.SetHeader(plugin.View, plugin.Plugin.ShortName);
                DocumentContainer.SetCanClose(plugin.View, false);
                Docking.Items.Add(plugin.View);
            }

            LoadDockingState();

            var args = new NotificationArgs {
                Detail = "Welcome, Commander. Bridge crew are standing by and awaiting your instructions."
            };

            if (_settings.InbuiltVoiceEnabled)
            {
                // Fire and forget to the inbuilt voice notifier
                Task.Delay(1000)
                    .ContinueWith(task => {
                        args.Suppression = NotificationSuppression.Title;
                        args.Rendering = NotificationRendering.NativeVocal;
                        _core.SendNotification(args);
                    });
            }
            else
            {
                // Fire and forget to plugin notifiers
                Task.Delay(1000)
                    .ContinueWith(task => {
                        args.Suppression = NotificationSuppression.Title;
                        args.Rendering = NotificationRendering.PluginNotifier;
                        _core.SendNotification(args);
                    });
            }
        }

        private void LoadDockingState()
        {
            if (File.Exists(_dockStateFile))
            {
                Docking.LoadDockState(_dockStateFile);
                foreach (var plugin in PluginViews)
                {
                    DocumentContainer.SetHeader(plugin.View, plugin.Plugin.ShortName);
                    DocumentContainer.SetCanClose(plugin.View, false);
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Docking.SaveDockState(_dockStateFile);

            if (this.WindowState == WindowState.Normal)
                _settings.MainWindowBounds = new WindowBounds((int)Left, (int)Top, (int)Width, (int)Height, (int)WindowState);
            else
                _settings.MainWindowBounds = new WindowBounds((int)RestoreBounds.Left, (int)RestoreBounds.Top, (int)RestoreBounds.Width, (int)RestoreBounds.Height, (int)WindowState);
            _settings.SaveSettings();
        }

        private void OnThemeClick(object menuItem, RoutedEventArgs e)
        {
            var button = (DropDownMenuItem)menuItem;
            var theme = (string)button!.CommandParameter;
            SfSkinManager.SetTheme(this, new Theme(theme));

            _settings.AppTheme = theme;
            _settings.SaveSettings();
        }

        private void OnThemeMenuOpenedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            DropDownButton button = (DropDownButton)sender;
            foreach(DropDownMenuItem item in button.Items.OfType<DropDownMenuItem>())
            {
                item.IsChecked = ((string)item.CommandParameter) == _settings.AppTheme;
            }
        }

        private void OnResetLayoutClick(object sender, RoutedEventArgs e)
        {
            Docking.DeleteDockState();
            Docking.LoadDockState();
        }


        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            var window = _core.Services.GetRequiredService<SettingsWindow>();
            window.Owner = this;
            window.ShowDialog();
        }
    }
}
