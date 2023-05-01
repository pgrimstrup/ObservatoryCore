using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Syncfusion.UI.Xaml.Grid.Cells;

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
        }

        private void OnAutoGeneratingColumn(object sender, Syncfusion.UI.Xaml.Grid.AutoGeneratingColumnArgs e)
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
                }
            }
        }
    }
}
