using System.IO;
using System.Windows;
using System.Windows.Controls;
using Observatory.Plugins;

namespace ObservatoryUI.WPF.Utils
{
    public class SettingTemplateSelector : DataTemplateSelector
    {
        public DataTemplate StringTemplate { get; set; } = null!;

        public DataTemplate BooleanTemplate { get; set; } = null!;

        public DataTemplate FileInfoTemplate { get; set; } = null!;

        public DataTemplate IntSliderTemplate { get; set; } = null!;

        public DataTemplate DoubleNumericTemplate { get; set; } = null!;

        public DataTemplate IntNumericTemplate { get; set; } = null!;

        public DataTemplate ActionTemplate { get; set; } = null!;

        public DataTemplate ComboBoxTemplate { get; set; } = null!;

        public DataTemplate HiddenTemplate { get; set; } = null!;

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var setting = item as SettingProperty;
            if (setting == null || setting.Hidden)
                return this.HiddenTemplate;

            if (setting.ValueProperty.PropertyType == typeof(FileInfo))
                return this.FileInfoTemplate;

            if (setting.ValueProperty.PropertyType == typeof(Action))
                return this.ActionTemplate;

            if (setting.Items != null)
                return this.ComboBoxTemplate;

            if (setting.ValueProperty.PropertyType == typeof(bool))
                return this.BooleanTemplate;

            if (setting.ValueProperty.PropertyType == typeof(Int16) ||
                setting.ValueProperty.PropertyType == typeof(Int32) ||
                setting.ValueProperty.PropertyType == typeof(Int64))
            {
                if(setting.UseIntSlider)
                    return this.IntSliderTemplate;
                else
                    return this.IntNumericTemplate;
            }

            if (setting.ValueProperty.PropertyType == typeof(float) ||
                setting.ValueProperty.PropertyType == typeof(double) ||
                setting.ValueProperty.PropertyType == typeof(decimal))
            {
                return this.DoubleNumericTemplate;
            }

            return this.StringTemplate;
        }
    }
}
