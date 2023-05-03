using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using Observatory.Framework;
using System.IO;
using System.Collections.ObjectModel;
using Observatory.Framework.Interfaces;

namespace ObservatoryUI.WPF.Views
{
    public class SettingProperty
    {
        public IObservatoryPlugin Plugin { get; set; }
        public object Settings { get; set; }
        public PropertyInfo ValueProperty { get; set; }
        public string DisplayName { get; set; }
        public bool Hidden { get; set; }

        public double MinimumValue { get; set; }
        public double MaximumValue { get; set; }
        public double Increment { get; set; }

        // If Items is not-null, the property will be rendered as a ComboBox. 
        public ObservableCollection<ComboData>? Items { get; set; } 

        public int Row { get; set; }
        public int Column { get; set; }
        public int ColumnSpan { get; set; } = 2;

        public object? Value
        {
            get => ValueProperty.GetValue(Settings);
            set => ValueProperty.SetValue(Settings, value);
        }

        public class ComboData
        {
            public string Name { get; set; } = "";
            public object Value { get; set; } = "";
        } 

        public SettingProperty(IObservatoryPlugin plugin, object settings, PropertyInfo property, SettingProperty? previous)
        {
            Plugin = plugin;
            Settings = settings;
            ValueProperty = property;

            var ignore = property.GetCustomAttribute<SettingIgnore>();
            Hidden = ignore != null;

            var displayName = property.GetCustomAttribute<SettingDisplayName>();
            DisplayName = displayName?.DisplayName ?? property.Name;

            MinimumValue = Int32.MinValue;
            MaximumValue = Int32.MaxValue;
            Increment = 1;

            var bounds = ValueProperty.GetCustomAttribute<SettingNumericBounds>();
            if (bounds != null)
            {
                if (bounds.Maximum > bounds.Minimum)
                {
                    MinimumValue = bounds.Minimum;
                    MaximumValue = bounds.Maximum;
                }
                if(bounds.Increment > 0)
                    Increment = bounds.Increment;
            }

            if (ValueProperty.PropertyType == typeof(Dictionary<string, object>))
            {
                var backingAttribute = property.GetCustomAttribute<SettingBackingValue>();
                if (backingAttribute != null)
                {
                    var backing = settings.GetType().GetProperty(backingAttribute.BackingProperty);
                    if (backing != null)
                    {
                        var itemsProperty = ValueProperty;

                        this.Items = new();
                        var items = (Dictionary<string, object>?)itemsProperty.GetValue(settings, null);
                        if (items != null)
                        {
                            foreach (var item in items)
                                Items.Add(new ComboData { Name = item.Key, Value = item.Value });
                        }

                        ValueProperty = backing;
                    }
                }
            }
            else
            {
                var getItemAttribute = property.GetCustomAttribute<SettingGetItemsMethod>();
                if (getItemAttribute != null && !String.IsNullOrEmpty(getItemAttribute.MethodName))
                {
                    var method = plugin.GetType().GetMethod(getItemAttribute.MethodName);
                    if (method != null && method.ReturnType == typeof(Dictionary<string, object>))
                    {
                        this.Items = new();
                        var items = (Dictionary<string, object>?)method.Invoke(plugin, null);
                        if (items != null)
                        {
                            foreach (var item in items)
                                Items.Add(new ComboData { Name = item.Key, Value = item.Value });
                        }

                    }
                }
            }

            if (previous != null)
            {
                if(property.PropertyType == typeof(Boolean) && previous.ValueProperty.PropertyType == typeof(Boolean))
                {
                    // Checkboxes can go into two columns
                    previous.ColumnSpan = 1;
                    if (previous.Column == 0)
                    {
                        this.Column = 1;
                        this.Row = previous.Row;
                        this.ColumnSpan = 1;
                    }
                    else
                    {
                        this.Column = 0;
                        this.Row = previous.Row + 1;
                        this.ColumnSpan = 1;
                    }
                }
                else
                {
                    // New row with column span 2
                    Row = previous.Row + 1;
                    Column = 0;
                    ColumnSpan = 2;

                    // Fixup the last control if it was left hanging
                    if (previous.Column == 0 && previous.ColumnSpan == 1)
                        previous.ColumnSpan = 2;
                }
            }
        }
    }

    public class SettingTemplateSelector : DataTemplateSelector
    {
        public DataTemplate StringTemplate { get; set; }

        public DataTemplate BooleanTemplate { get; set; }

        public DataTemplate FileInfoTemplate { get; set; }

        public DataTemplate IntSliderTemplate { get; set; }

        public DataTemplate NumericTemplate { get; set; }

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
                setting.ValueProperty.PropertyType == typeof(Int32)|| 
                setting.ValueProperty.PropertyType == typeof(Int64) ||
                setting.ValueProperty.PropertyType == typeof(float) || 
                setting.ValueProperty.PropertyType == typeof(double) ||
                setting.ValueProperty.PropertyType == typeof(decimal))
            {
                var useSlider = setting.ValueProperty.GetCustomAttribute<SettingNumericUseSlider>();
                if (useSlider == null || setting.Increment == 0 || (setting.MaximumValue - setting.MinimumValue) / setting.Increment > 100)
                    return this.NumericTemplate;
                else
                    return this.IntSliderTemplate;
            }

            return this.StringTemplate;
        }
    }
}
