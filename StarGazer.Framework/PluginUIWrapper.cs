using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework;

namespace StarGazer.Framework
{
    /// <summary>
    /// A data-binding wrapper around the PluginUI class
    /// </summary>
    public class PluginUIWrapper : INotifyPropertyChanged
    {
        PluginUI _pluginUI;
        object _selectedItem;

        public event PropertyChangedEventHandler PropertyChanged;

        public PluginUI PluginUI => _pluginUI;

        public PluginUIWrapper(PluginUI pluginUI)
        {
            _pluginUI = pluginUI;
        }

        /// <summary>
        /// <para>Collection bound to DataGrid used byu plugins with UIType.Basic.</para>
        /// <para>Objects in collection should be of a class defined within the plugin consisting of string properties.<br/>Each object is a single row, and the property names are used as column headers.</para>
        /// </summary>
        public ObservableCollection<object> DataGrid
        {
            get => _pluginUI.DataGrid;
            set
            {
                if(_pluginUI.DataGrid != value)
                {
                    _pluginUI.DataGrid = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DataGrid)));
                }
            }
        }

        public object SelectedItem
        {
            get => _selectedItem;
            set
            {
                if(_selectedItem != value)
                {
                    _selectedItem = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
                }
            }
        }

    }
}
