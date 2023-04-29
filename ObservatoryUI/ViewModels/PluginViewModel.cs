using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Interfaces;
using ObservatoryUI.Views;
using Syncfusion.Maui.DataGrid;

namespace ObservatoryUI.ViewModels
{
    public class PluginViewModel
    {
        readonly PluginView _view;
        readonly IObservatoryPlugin _plugin;
        List<PluginColumnInfo> _columns;

        public int Row { get; set; }
        public int Column { get; set; }
        public PluginView View => _view;
        public IObservatoryPlugin Plugin => _plugin;


        public PluginViewModel(IObservatoryPlugin plugin, PluginView view)
        {
            _plugin = plugin;
            _view = view;

            _plugin.PluginUI.DataGrid.CollectionChanged += DataGrid_CollectionChanged;
            _view.BindingContext = _plugin.PluginUI.DataGrid;
        }

        private void DataGrid_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Count > 0 && _columns == null)
            {
                _columns = PopulateColumnInfo(e.NewItems[0]);
                View.CreateDataColumns(_columns);
            }
        }

        private List<PluginColumnInfo> PopulateColumnInfo(object item)
        {
            Type type = item.GetType();
            List<PluginColumnInfo> columns = new List<PluginColumnInfo>();

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
                    if (!String.IsNullOrEmpty(display.Name))
                        columnInfo.HeaderText = display.Name;
                }

                var format = property.GetCustomAttribute<DisplayFormatAttribute>();
                if (format != null)
                {
                    columnInfo.DisplayFormat = format.DataFormatString;
                    columnInfo.DisplayNullValue = format.NullDisplayText;
                }

                var propType = property.PropertyType;
                if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    propType = propType.GetGenericArguments().First();

                if (propType == typeof(int) ||
                    propType == typeof(float) ||
                    propType == typeof(double) ||
                    propType == typeof(decimal))
                {
                    columnInfo.GridColumnType = typeof(DataGridNumericColumn);
                }
                else if (propType == typeof(DateTime) ||
                    propType == typeof(DateTimeOffset))
                {
                    columnInfo.GridColumnType = typeof(DataGridDateColumn);
                }
                else if (propType == typeof(bool))
                {
                    columnInfo.GridColumnType = typeof(DataGridCheckBoxColumn);
                }
                else if (propType == typeof(Bitmap) || propType == typeof(Icon))
                {
                    columnInfo.GridColumnType = typeof(DataGridImageColumn);
                }
                else if (propType == typeof(string))
                {
                    columnInfo.GridColumnType = typeof(DataGridTextColumn);
                }

                columns.Add(columnInfo);
            }

            return columns;

        }

    }
}
