using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;
using Syncfusion.Windows.Shared;

namespace ObservatoryUI.WPF.Views
{
    /// <summary>
    /// Interaction logic for PluginView.xaml
    /// </summary>
    public partial class PluginView : UserControl
    {
        GridRowSizingOptions rowSizingOptions = new GridRowSizingOptions();

        public event EventHandler FontSizeChanged;

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
                double width = base.CalculateCellWidth(column, setWidth);
                if (width < column.MinimumWidth)
                    width = column.MinimumWidth;
                return width + 8;
            }

            //Calculate Width for the column when ColumnSizer is SizeToHeader
            protected override double CalculateHeaderWidth(GridColumn column, bool setWidth = true)
            {
                base.FontSize = base.DataGrid.FontSize;
                double headerWidth = base.CalculateHeaderWidth(column, setWidth);
                if (headerWidth < column.MinimumWidth)
                    headerWidth = column.MinimumWidth;

                double cellWidth = base.CalculateCellWidth(column, setWidth);
                if (headerWidth < cellWidth)
                    return cellWidth + 8;

                return headerWidth + 8;
            }
        }

        private void DataGrid_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if(Keyboard.Modifiers == ModifierKeys.Control)
            {
                double fontSize = DataGrid.FontSize;
                fontSize += e.Delta / 120.0;
                DataGrid.FontSize = fontSize;
                ResetColumnRowSizes();
                DataGrid.GridColumnSizer.Refresh();
                e.Handled = true;

                FontSizeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ResetColumnRowSizes()
        {
            DataGrid.GetVisualContainer()?.RowHeightManager?.Reset();
            DataGrid.GetVisualContainer()?.InvalidateMeasureInfo();
            DataGrid.GridColumnSizer?.ResetAutoCalculationforAllColumns();
        }

        private void DataGrid_QueryRowHeight(object sender, QueryRowHeightEventArgs e)
        {
            e.Height = DataGrid.FontSize * 1.4 + 4;
            e.Handled = true;
        }
    }
}
