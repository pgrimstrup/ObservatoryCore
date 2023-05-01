using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Observatory.Framework.Interfaces;
using ObservatoryUI.WPF.Views;
using Syncfusion.Windows.Tools.Controls;

namespace ObservatoryUI.WPF.ViewModels
{
    public class PluginViewModel 
    {
        readonly IObservatoryPlugin _plugin;
        readonly PluginView _view;

        ObservableCollection<PluginColumnInfo> _columns = new ObservableCollection<PluginColumnInfo>();

        public int Row { get; set; }
        public int Column { get; set; }
        public PluginView View => _view;
        public IObservatoryPlugin Plugin => _plugin;

        public IList<PluginColumnInfo> DataGridColumns => _columns;

        public PluginViewModel(IObservatoryPlugin plugin, PluginView view)
        {
            _plugin = plugin;
            _view = view;

            _plugin.PluginUI.DataGrid.CollectionChanged += DataGrid_CollectionChanged;

            _view.DataContext = _plugin.PluginUI.DataGrid;
        }

        private void DataGrid_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
        }
    }
}
