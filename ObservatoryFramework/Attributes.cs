﻿using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Framework
{
    // These Attribute classes are all named incorrectly. They do not have the "Attribute" suffix to their class name.
    // They are kept to maintain backward compatibility with other plugins.
    // THey will be flagged as obsolete and replaced with DataAnnotationAttributes.

    /// <summary>
    /// Specifies text to display as the name of the setting in the UI instead of the property name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SettingDisplayName : Attribute
    {
        private string name;

        /// <summary>
        /// Specifies text to display as the name of the setting in the UI instead of the property name.
        /// </summary>
        /// <param name="name">Name to display</param>
        public SettingDisplayName(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Accessor to get/set displayed name.
        /// </summary>
        public string DisplayName
        {
            get => name;
            set => name = value;
        }
    }


    /// <summary>
    /// Indicates that the property should not be displayed to the user in the UI.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SettingIgnore: Attribute
    { }

    /// <summary>
    /// Indicates numeric properly should use a slider control instead of a numeric textbox with roller.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SettingNumericUseSlider: Attribute
    { }

    /// <summary>
    /// Specify backing value used by Dictionary&lt;string, object&gt; to indicate selected option.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SettingBackingValue : Attribute
    {
        /// <summary>
        /// Specify backing value used by Dictionary&lt;string, object&gt; to indicate selected option.
        /// </summary>
        /// <param name="property">Property name for backing value.</param>
        public SettingBackingValue(string property)
        {
            this.BackingProperty = property;
        }

        /// <summary>
        /// Accessor to get/set backing value property name.
        /// </summary>
        public string BackingProperty { get; set; }
    }

    /// <summary>
    /// Instead of the SettingBackingValueAttribute, a method name on the Plugin can be provided that
    /// will return the Dictionary of values to be used to populate the drop down list. The
    /// Setting field is used to store the selected value. This attribute will always convert the 
    /// setting to a ComboBox selection. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SettingGetItemsMethod: Attribute
    {
        public SettingGetItemsMethod(string methodName)
        {
            this.MethodName = methodName;
        }

        public string MethodName { get; set; }
    }

    /// <summary>
    /// Specify bounds for numeric inputs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SettingNumericBounds: Attribute
    {
        private double minimum;
        private double maximum;
        private double increment;

        /// <summary>
        /// Specify bounds for numeric inputs.
        /// </summary>
        /// <param name="minimum">Minimum allowed value.</param>
        /// <param name="maximum">Maximum allowed value.</param>
        /// <param name="increment">Increment between allowed values in slider/roller inputs.</param>
        public SettingNumericBounds(double minimum, double maximum, double increment = 1.0)
        {
            this.minimum = minimum;
            this.maximum = maximum;
            this.increment = increment;
        }

        /// <summary>
        /// Minimum allowed value.
        /// </summary>
        public double Minimum
        {
            get => minimum;
            set => minimum = value;
        }

        /// <summary>
        /// Maxunyn allowed value.
        /// </summary>
        public double Maximum
        {
            get => maximum;
            set => maximum = value;
        }

        /// <summary>
        /// Increment between allowed values in slider/roller inputs.
        /// </summary>
        public double Increment
        {
            get => increment;
            set => increment = value;
        }
    }
}
