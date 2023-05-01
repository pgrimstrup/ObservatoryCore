using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
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
        
        public ObservableCollection<DockItem> PluginViews { get; } = new ObservableCollection<DockItem>();

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
            LoadDockingState();
        }

        private void LoadDockingState()
        {
            if (File.Exists(_dockStateFile))
            {
                var items = PluginViews.ToArray();
                foreach(var item in items)
                {
                    item.CanAutoHide = true;
                    item.CanClose = true;
                }

                if (!Docking.LoadDockState(_dockStateFile))
                {
                    Docking.ResetState();
                }

                foreach (var item in items)
                {
                    if (item.State == DockState.Hidden)
                        item.State = DockState.Document;
                    item.CanAutoHide = false;
                    item.CanClose = false;
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
            var button = menuItem as DropDownMenuItem;
            var theme = (string)button.CommandParameter;
            SfSkinManager.SetTheme(this, new Theme(theme));

            _settings.AppTheme = theme;
            _settings.SaveSettings();
        }

        private void OnThemeMenuOpenedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            DropDownButton button = sender as DropDownButton;
            foreach(DropDownMenuItem item in button.Items.OfType<DropDownMenuItem>())
            {
                item.IsChecked = ((string)item.CommandParameter) == _settings.AppTheme;
            }
            
        }
    }
}
