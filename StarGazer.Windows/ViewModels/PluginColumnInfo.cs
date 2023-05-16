using System.Drawing;
using System.Reflection;

namespace StarGazer.UI.ViewModels
{
    public class PluginColumnInfo
    {
        PropertyInfo _property = null!;
        Type _valueType = null!;

        public PropertyInfo PropertyInfo 
        {
            get => _property;
            set
            {
                _property = value;
                var propType = PropertyInfo.PropertyType;
                if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    propType = propType.GetGenericArguments().First();
                _valueType = propType;
            }
        }

        public string HeaderText { get; set; } = "";

        public bool DisplayField { get; set; } = true;
        public bool DisplayFilter { get; set; } = true;

        public string DisplayFormat { get; set; } = "{0}";
        public string DisplayNullValue { get; set; } = "";

        public bool IsTextColumn => !IsImageColumn && !IsCheckboxColumn;
        public bool IsImageColumn => _valueType == typeof(Icon) || _valueType == typeof(Bitmap);
        public bool IsCheckboxColumn => _valueType == typeof(bool);

    }
}
