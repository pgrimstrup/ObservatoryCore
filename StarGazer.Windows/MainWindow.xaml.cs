using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarGazer.Framework.Interfaces;
using StarGazer.UI.ViewModels;
using StarGazer.UI.Views;
using Syncfusion.Data.Extensions;
using Syncfusion.SfSkinManager;
using Syncfusion.Windows.Tools.Controls;

namespace StarGazer.UI
{
    /// <summary>
    /// Interaction logic for ChromelessWindow1.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        readonly IStarGazerCore _core;
        readonly IAppSettings _settings;
        readonly string _dockStateFile;
        readonly ILogger _logger;

        public ObservableCollection<PluginViewModel> PluginViews { get; } = new ObservableCollection<PluginViewModel>();

        public MainWindow(IStarGazerCore core, IAppSettings settings, ILogger<MainWindow> logger)
        {
            SfSkinManager.SetTheme(this, new Theme(settings.AppTheme));
            InitializeComponent();

            _core = core;
            _settings = settings;
            _logger = logger;
            _dockStateFile = Path.Combine(_core.CoreFolder, "docking_state.xml");

            _core.LoadCoreSettings();
            if (!_settings.MainWindowBounds.IsEmpty)
            {
                this.Left = _settings.MainWindowBounds.X;
                this.Top = _settings.MainWindowBounds.Y;
                this.Width = _settings.MainWindowBounds.Width;
                this.Height = _settings.MainWindowBounds.Height;
            }
        }

        protected async void OnLoadedAsync(object sender, RoutedEventArgs e)
        {
            Ribbon.BackStageButton.Visibility = Visibility.Collapsed;
            if (!_settings.MainWindowBounds.IsEmpty)
                this.WindowState = (WindowState)_settings.MainWindowBounds.State;

            var plugins = await _core.InitializeAsync();
            foreach (var plugin in plugins.Where(p => p.PluginUI?.DataGrid != null))
            {
                var view = _core.Services.GetRequiredService<PluginView>();
                var viewModel = new PluginViewModel(plugin, view);

                // Font size changes on the grid
                view.FontSizeChanged += (s, e) => {
                    _settings.GridFontSizes[plugin.GetType().FullName ?? ""] = view.DataGrid.FontSize;
                };

                // Speaking of which, initialize the font size
                if (_settings.GridFontSizes.TryGetValue(plugin.GetType().FullName ?? "", out var size))
                {
                    view.DataGrid.FontSize = size;
                    view.ResetColumnRowSizes();
                }

                PluginViews.Add(viewModel);
            }

            LoadDockingState();

            this.DataContext = this;
            lblLoading.Visibility = Visibility.Hidden;
            Documents.Visibility = Visibility.Visible;

            _logger.LogInformation("MainWindow load has completed");
            await Task.CompletedTask;
        }

        private void LoadDockingState()
        {
            // Load the state first. We need to fix it if needed based on what plugins we have
            List<PluginViewModel> loaded = new List<PluginViewModel>();
            List<PluginViewModel> unloaded = PluginViews.ToList();

            if (File.Exists(_dockStateFile))
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(_dockStateFile);

                var nodes = xml.SelectNodes("//DocumentParamsBase").ToList<XmlNode>().ToList();

                // Sort the nodes by TDIIndex
                nodes.Sort((a, b) => {
                    Int32.TryParse(a["TDIIndex"]?.InnerText, out int index_a);
                    Int32.TryParse(b["TDIIndex"]?.InnerText, out int index_b);
                    return Comparer<int>.Default.Compare(index_a, index_b);
                });

                foreach (XmlNode node in nodes)
                {
                    string? name = node["Name"]?.InnerText;

                    // Find a plugin with the same name
                    var plugin = unloaded.FirstOrDefault(p => p.Plugin.GetType().FullName!.Replace(".", "_") == name);
                    if (plugin != null)
                    {
                        loaded.Add(plugin);
                        unloaded.Remove(plugin);
                    }
                }

                // Before loading the state, add in all of the plugins referenced in the docking state
                foreach (var viewModel in loaded)
                {
                    Documents.Items.Add(viewModel.View);
                    DocumentContainer.SetHeader(viewModel.View, viewModel.Plugin.ShortName);
                }

                Documents.LoadDockState(_dockStateFile);
            }

            // Now add in any tabs that were not referenced in the docking state
            foreach (var viewModel in unloaded)
            {
                Documents.Items.Add(viewModel.View);
                DocumentContainer.SetHeader(viewModel.View, viewModel.Plugin.ShortName);
            }
            _logger.LogInformation("MainWindow Docking State has been loaded");
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _logger.LogInformation("Closing MainWindow");
            Documents.SaveDockState(_dockStateFile);

            // Save the location of the main window and all other settings, in case they changed
            if (this.WindowState == WindowState.Normal)
                _settings.MainWindowBounds = new WindowBounds((int)Left, (int)Top, (int)Width, (int)Height, (int)WindowState);
            else
                _settings.MainWindowBounds = new WindowBounds((int)RestoreBounds.Left, (int)RestoreBounds.Top, (int)RestoreBounds.Width, (int)RestoreBounds.Height, (int)WindowState);
            _core.SaveCoreSettings();

        }

        private void OnThemeClick(object menuItem, RoutedEventArgs e)
        {
            var button = (DropDownMenuItem)menuItem;
            var theme = (string)button!.CommandParameter;
            SfSkinManager.SetTheme(this, new Theme(theme));

            _settings.AppTheme = theme;
            _core.SaveCoreSettings();
        }

        private void OnThemeMenuOpenedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            DropDownButton button = (DropDownButton)sender;
            foreach (DropDownMenuItem item in button.Items.OfType<DropDownMenuItem>())
            {
                item.IsChecked = ((string)item.CommandParameter) == _settings.AppTheme;
            }
        }

        private void OnResetLayoutClick(object sender, RoutedEventArgs e)
        {
            Documents.ResetState();
        }


        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            var window = _core.Services.GetRequiredService<SettingsWindow>();
            window.Owner = this;
            window.ShowDialog();
        }

        private async void OnReadAllClick(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;

            var logMonitor = _core.Services.GetRequiredService<ILogMonitor>();
            await logMonitor.ReadAllAsync();

            this.Cursor = Cursors.Arrow;
        }

        private async void OnReadCurrentClick(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;

            var logMonitor = _core.Services.GetRequiredService<ILogMonitor>();
            await logMonitor.ReadCurrentAsync();

            this.Cursor = Cursors.Arrow;
        }

        private void OnPluginsFolderClick(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo info = new ProcessStartInfo {
                Verb = "open",
                FileName = _core.PluginStorageFolder,
                WorkingDirectory = _core.PluginStorageFolder,
                UseShellExecute = true
            };

            Process.Start(info);
        }
    }
}
