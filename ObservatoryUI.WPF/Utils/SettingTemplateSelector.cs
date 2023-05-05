using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework;
using Observatory.Plugins;
using System.Windows.Controls;
using System.Windows;

namespace ObservatoryUI.WPF.Utils
{
    public class SettingTemplateSelector : DataTemplateSelector
    {
        public DataTemplate StringTemplate { get; set; }

        public DataTemplate BooleanTemplate { get; set; }

        public DataTemplate FileInfoTemplate { get; set; }

        public DataTemplate IntSliderTemplate { get; set; }

        public DataTemplate DoubleNumericTemplate { get; set; }

        public DataTemplate IntNumericTemplate { get; set; }

        public DataTemplate ActionTemplate { get; set; }

        public DataTemplate ComboBoxTemplate { get; set; }

        public DataTemplate HiddenTemplate { get; set; }

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
