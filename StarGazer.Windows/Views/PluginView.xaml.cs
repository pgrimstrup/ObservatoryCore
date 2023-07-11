using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;
using Observatory.Framework;
using StarGazer.Framework;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;
using Syncfusion.UI.Xaml.ScrollAxis;

namespace StarGazer.UI.Views
{
    /// <summary>
    /// Interaction logic for PluginView.xaml
    /// </summary>
    public partial class PluginView : UserControl
    {
        GridRowSizingOptions rowSizingOptions = new GridRowSizingOptions();

        public event EventHandler FontSizeChanged = null!;

        public PluginView()
        {
            InitializeComponent();
            rowSizingOptions.AutoFitMode = AutoFitMode.Default;
            DataGrid.GridColumnSizer = new CustomGridColumnSizer(DataGrid);
        }

        private void OnAutoGeneratingColumn(object sender, AutoGeneratingColumnArgs e)
        {
            var items = DataContext as IList;
            if(items != null && items.Count > 0)
            {
                Type type = items[0]!.GetType();
                var property = type.GetProperty(e.Column.MappingName);
                if(property != null)
                {
                    var display = property.GetCustomAttribute<DisplayAttribute>();
                    if(display != null)
                    {
                        e.Column.HeaderText = display.Name ?? e.Column.HeaderText;
                    }

                    e.Column.Width = double.NaN; // Force recalculation
                }
            }
        }

        public class CustomGridColumnSizer : GridColumnSizer
        {
            public CustomGridColumnSizer(SfDataGrid dataGrid)
                : base(dataGrid)
            {
            }

            // Calculate Width for column when ColumnSizer is SizeToCells.        
            protected override double CalculateCellWidth(GridColumn column, bool setWidth = true)
            {
                double width = base.CalculateCellWidth(column, setWidth) + column.Padding.Left + column.Padding.Right + 10;
                if (column.MaximumWidth != double.NaN && width > column.MaximumWidth)
                    width = column.MaximumWidth;
                if (column.MinimumWidth != double.NaN && width < column.MinimumWidth)
                    width = column.MinimumWidth;

                return width;
            }

            //Calculate Width for the column when ColumnSizer is SizeToHeader
            protected override double CalculateHeaderWidth(GridColumn column, bool setWidth = true)
            {
                base.FontSize = base.DataGrid.FontSize;
                double headerWidth = base.CalculateHeaderWidth(column, setWidth);
                double cellWidth = base.CalculateCellWidth(column, setWidth);

                if (headerWidth < cellWidth)
                    cellWidth = cellWidth + column.Padding.Left + column.Padding.Right + 10;
                else
                    cellWidth = headerWidth + column.Padding.Left + column.Padding.Right + 10;

                if(column.MaximumWidth != Double.NaN && cellWidth > column.MaximumWidth)
                    cellWidth = column.MaximumWidth;
                if (column.MinimumWidth != Double.NaN && cellWidth < column.MinimumWidth)
                    cellWidth = column.MinimumWidth;

                return cellWidth;
            }
        }

        private void DataGrid_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if(Keyboard.Modifiers == ModifierKeys.Control)
            {
                double fontSize = DataGrid.FontSize;
                fontSize += e.Delta / 120.0;
                if (fontSize > 6 && fontSize < 30)
                {
                    DataGrid.FontSize = fontSize;
                    ResetColumnWidths();
                    ResetColumnRowSizes();
                    FontSizeChanged?.Invoke(this, EventArgs.Empty);
                }
                e.Handled = true;

            }
        }

        public void ResetColumnRowSizes()
        {
            DataGrid.GetVisualContainer()?.RowHeightManager?.Reset();
            DataGrid.GetVisualContainer()?.InvalidateMeasureInfo();
            DataGrid.GridColumnSizer?.ResetAutoCalculationforAllColumns();
        }

        public void ResetColumnWidths()
        {
            foreach (var col in DataGrid.Columns)
                col.Width = Double.NaN;
            DataGrid.GridColumnSizer.Refresh();
        }

        private void DataGrid_QueryRowHeight(object sender, QueryRowHeightEventArgs e)
        {
            e.Height = DataGrid.FontSize * 1.4 + 4;
            e.Handled = true;
        }

        private void DataGrid_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var pluginUI = DataGrid.DataContext as PluginUIWrapper;
            if (pluginUI == null)
                return;

            pluginUI.DataGrid.CollectionChanged += CollectionChanged;
            pluginUI.SelectedItem = pluginUI.DataGrid.LastOrDefault();
        }

        private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            var pluginUI = DataGrid.DataContext as PluginUIWrapper;
            if (pluginUI == null)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null && e.NewItems.Count > 0)
                    {
                        ResetColumnWidths();
                        SelectLastItem(pluginUI, e.NewItems.Cast<object>());
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null && e.OldItems.Count > 0)
                    {
                        if (e.OldItems.Contains(pluginUI.SelectedItem))
                        {
                            pluginUI.SelectedItem = null;
                        }
                        ResetColumnWidths();
                    }
                    break;

            }
        }

        private void SelectLastItem(PluginUIWrapper pluginUI, IEnumerable<object> items)
        {
            if (items != null && items.Any())
            {
                var selected = items.Last();
                pluginUI.SelectedItem = selected;

                var rowindex = DataGrid.ResolveToRowIndex(selected);
                if (rowindex < 0)
                    return;

                DataGrid.ScrollInView(new RowColumnIndex(rowindex, 0));
            }
        }


    }
}
