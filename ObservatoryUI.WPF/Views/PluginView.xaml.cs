using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Windows.Controls;
using Syncfusion.UI.Xaml.Grid;

namespace ObservatoryUI.WPF.Views
{
    /// <summary>
    /// Interaction logic for PluginView.xaml
    /// </summary>
    public partial class PluginView : UserControl
    {
        public PluginView()
        {
            InitializeComponent();
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

                    //e.Column.MinimumWidth = 30;
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
                return width;
            }

            //Calculate Width for the column when ColumnSizer is SizeToHeader
            protected override double CalculateHeaderWidth(GridColumn column, bool setWidth = true)
            {
                double headerWidth = base.CalculateHeaderWidth(column, setWidth);
                if (headerWidth < column.MinimumWidth)
                    headerWidth = column.MinimumWidth;

                double cellWidth = base.CalculateCellWidth(column, setWidth);
                if (headerWidth < cellWidth)
                    return cellWidth;

                return headerWidth;
            }
        }
    }
}
