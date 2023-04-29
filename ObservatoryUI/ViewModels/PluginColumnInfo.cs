using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ObservatoryUI.ViewModels
{
    public class PluginColumnInfo
    {
        public PropertyInfo PropertyInfo { get; set; }

        public Type GridColumnType { get; set; }

        public string HeaderText { get; set; }

        public bool DisplayField { get; set; } = true;
        public bool DisplayFilter { get; set; } = true;

        public string DisplayFormat { get; set; }
        public string DisplayNullValue { get; set; }

    }
}
