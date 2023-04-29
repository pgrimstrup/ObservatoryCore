using ObservatoryUI.ViewModels;
using Syncfusion.Maui.DataGrid;

namespace ObservatoryUI.Views;

public partial class PluginView : ContentView
{
	public PluginView()
	{
		InitializeComponent();
	}

    public void CreateDataColumns(List<PluginColumnInfo> columns)
    {
        DataGrid.Columns.Clear();
        foreach(var column in columns)
        {
            if (column.GridColumnType == null || !column.DisplayField)
                continue;

            DataGridColumn gridcolumn = (DataGridColumn)Activator.CreateInstance(column.GridColumnType);
            gridcolumn.HeaderText = column.HeaderText;
            gridcolumn.MappingName = column.PropertyInfo.Name;
            gridcolumn.Format = column.DisplayFormat;
            gridcolumn.ColumnWidthMode = ColumnWidthMode.FitByCell;
            gridcolumn.MinimumWidth = 20;

            DataGrid.Columns.Add(gridcolumn);
        }
    }
}