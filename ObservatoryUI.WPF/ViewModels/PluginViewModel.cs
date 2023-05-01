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
    public class PluginViewModel : DockItem
    {
        readonly IObservatoryPlugin _plugin;
        ObservableCollection<PluginColumnInfo> _columns = new ObservableCollection<PluginColumnInfo>();

        public int Row { get; set; }
        public int Column { get; set; }
        public PluginView View => (PluginView)Content;
        public IObservatoryPlugin Plugin => _plugin;

        public IList<PluginColumnInfo> DataGridColumns => _columns;

        public PluginViewModel(IObservatoryPlugin plugin, PluginView view)
        {
            _plugin = plugin;

            _plugin.PluginUI.DataGrid.CollectionChanged += DataGrid_CollectionChanged;

            Name = plugin.GetType().FullName!.Replace('.', '_');
            CanMaximize = false;
            CanMinimize = false;
            CanFloat = true;
            CanDocument = true;
            CanDock = false;
            CanAutoHide = true;

            Header = plugin.ShortName;
            State = DockState.Document;

            Content = view;
            Content.DataContext = _plugin.PluginUI.DataGrid;
        }

        private void DataGrid_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Count > 0 && _columns.Count == 0)
            {
                PopulateColumnInfo(e.NewItems[0]!);
            }
        }

        private void PopulateColumnInfo(object item)
        {
            Type type = item.GetType();

            foreach (var property in type.GetProperties())
            {
                PluginColumnInfo columnInfo = new PluginColumnInfo();
                columnInfo.PropertyInfo = property;
                columnInfo.HeaderText = property.Name;

                var display = property.GetCustomAttribute<DisplayAttribute>();
                if (display != null)
                {
                    columnInfo.DisplayField = display.AutoGenerateField;
                    columnInfo.DisplayFilter = display.AutoGenerateFilter;
                    if (!string.IsNullOrEmpty(display.Name))
                        columnInfo.HeaderText = display.Name;
                }

                var format = property.GetCustomAttribute<DisplayFormatAttribute>();
                if (format != null)
                {
                    columnInfo.DisplayFormat = format.DataFormatString ?? columnInfo.DisplayFormat;
                    columnInfo.DisplayNullValue = format.NullDisplayText ?? columnInfo.DisplayNullValue;
                }

                _columns.Add(columnInfo);
            }
        }

    }
}
