using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Observatory.Framework;
using Observatory.Framework.Interfaces;
using Observatory.Settings;

namespace Observatory.Plugins
{
    public class SettingProperty : INotifyPropertyChanged
    {
        public IObservatoryPlugin Plugin { get; private set; }
        public object Settings { get; private set; }
        public PropertyInfo ValueProperty { get; private set; }
        public PropertyInfo GetItemsProperty { get; private set; }
        public MethodInfo GetItemsMethod { get; private set; }
        public MethodInfo PluginActionMethod { get; private set; }
        public string DisplayName { get; private set; }
        public bool Hidden { get; private set; }
        public bool UseIntSlider { get; private set; }
        public string DependsOnPropertyName { get; private set; }
        
        public double MinimumValue { get; private set; }
        public double MaximumValue { get; private set; }
        public double Increment { get; private set; }

        // If Items is not-null, the property will be rendered as a ComboBox. 
        public ObservableCollection<NameValue> Items => _items.Value;

        // These can be changed by UI if needed
        public int Row { get; set; }
        public int Column { get; set; }
        public int ColumnSpan { get; set; } = 2;

        private Lazy<ObservableCollection<NameValue>> _items;
        
        public event PropertyChangedEventHandler PropertyChanged;

        public object Value
        {
            get => ValueProperty.GetValue(Settings);
            set
            {
                if (Value != value)
                {
                    ValueProperty.SetValue(Settings, value);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }
        }

        private SettingProperty(IObservatoryPlugin plugin, object settings, PropertyInfo property, SettingProperty previous)
        {
            Plugin = plugin;
            Settings = settings;
            ValueProperty = property;
            _items = new Lazy<ObservableCollection<NameValue>>(GetItems);

            var dependsOn = property.GetCustomAttribute<SettingDependsOn>();
            DependsOnPropertyName = dependsOn?.DependsOn;
            
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

            if (ValueProperty.PropertyType == typeof(Action))
            {
                var actionAttribute = property.GetCustomAttribute<SettingPluginAction>();
                if(actionAttribute != null)
                {
                    PluginActionMethod = Plugin.GetType().GetMethod(actionAttribute.MethodName, new Type[] { typeof(object) });
                    if(PluginActionMethod == null)
                        PluginActionMethod = Plugin.GetType().GetMethod(actionAttribute.MethodName);
                }
            }

            var slider = ValueProperty.GetCustomAttribute<SettingNumericUseSlider>();
            if(slider != null)
            {
                UseIntSlider = Increment > 0 && (MaximumValue - MinimumValue) / Increment <= 100;
            }

            if (ValueProperty.PropertyType == typeof(Dictionary<string, object>))
            {
                var backingAttribute = property.GetCustomAttribute<SettingBackingValue>();
                if (backingAttribute != null)
                {
                    var backing = settings.GetType().GetProperty(backingAttribute.BackingProperty);
                    if (backing != null)
                    {
                        GetItemsProperty = ValueProperty;
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
                        GetItemsMethod = method;
                    }
                    if (method != null && method.ReturnType == typeof(Task<Dictionary<string, object>>))
                    {
                        GetItemsMethod = method; 
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

        public async Task DoAction()
        {
            try
            {
                if (PluginActionMethod != null)
                {
                    Task task = (Task)PluginActionMethod.Invoke(Plugin, new object[] { Settings });
                    await task;
                }
                else
                {
                    var value = ValueProperty.GetValue(Settings, null);
                    if (value is Action action)
                        action.Invoke();
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void DependentPropertyChanged()
        {
            if (_items.IsValueCreated && Items != null)
            {
                // Track current value
                var currentValue = Convert.ToString(Value);

                // Get new list of items and repopulate the existing list
                var updated = GetItems();
                Items.Clear();
                foreach (var item in updated)
                    Items.Add(item);

                // Reset the selected value
                if (updated.Any(v => v.Name == currentValue))
                    Value = currentValue;
                else
                    Value = updated.FirstOrDefault()?.Name;
            }
        }

        private ObservableCollection<NameValue> GetItems()
        {
            try
            {
                object value = null;
                if (GetItemsMethod != null)
                {
                    var parameters = GetItemsMethod.GetParameters();
                    if (GetItemsMethod.ReturnType == typeof(Dictionary<string, object>))
                    {
                        // Call the non-async GetItems method 
                        if (parameters != null && parameters.Length == 0)
                            value = GetItemsMethod.Invoke(Plugin, null);
                        else
                            value = GetItemsMethod.Invoke(Plugin, new object[] { Settings });
                    }
                    else if (GetItemsMethod.ReturnType == typeof(Task<Dictionary<string, object>>))
                    {
                        if (parameters != null && parameters.Length == 0)
                        {
                            // Call the async GetItems method with no parameters
                            value = Task.Run(async () => {
                                var task = GetItemsMethod.Invoke(Plugin, null);
                                if (task == null)
                                    return null;
                                return await (Task<Dictionary<string, object>>)task;
                            }).GetAwaiter().GetResult();
                        }
                        else
                        {
                            // Call the async GetItems method passing in the current Settings object
                            value = Task.Run(async () => {
                                var task = GetItemsMethod.Invoke(Plugin, new object[] { Settings });
                                if (task == null)
                                    return null;
                                return await (Task<Dictionary<string, object>>)task;
                            }).GetAwaiter().GetResult();
                        }
                    }
                }
                else if (GetItemsProperty != null)
                {
                    // Get the items using the list property
                    value = GetItemsProperty.GetValue(Settings, null);
                }

                if (value is Dictionary<string, object> items)
                {
                    var result = new ObservableCollection<NameValue>();
                    foreach (var item in items)
                        result.Add(new  (item.Key, item.Value));
                    return result;
                }

                return null;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }

        public static IEnumerable<SettingProperty> CreateSettingProperties(IObservatoryPlugin plugin, object settings)
        {
            SettingProperty previous = null;

            List<SettingProperty> properties = new List<SettingProperty>();
            foreach (var property in settings.GetType().GetProperties())
            {
                var current = new SettingProperty(plugin, settings, property, previous);
                properties.Add(current);
                if (current.Hidden)
                    continue;

                yield return current;
                previous = current;
            }

            foreach (var property in properties)
            {
                if(property.DependsOnPropertyName != null)
                {
                    var depends = properties.FirstOrDefault(p => p.ValueProperty.Name == property.DependsOnPropertyName);
                    if(depends != null)
                    {
                        // Notify this property when the property value it depends on has changed
                        depends.PropertyChanged += (sender, e) => property.DependentPropertyChanged();
                    }
                }
            }
        }
    }
}
