using System;
using System.Collections.Generic;
using System.Linq;
using Observatory.PluginManagement;

namespace Observatory.UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel(PluginCore pluginCore)
        {
            core = new CoreViewModel(pluginCore);
        }

        public CoreViewModel core { get; }
    }
}
