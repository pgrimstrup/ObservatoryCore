namespace StarGazer.Framework
{
    /// <summary>
    /// Specifies text to display as the name of the setting in the UI instead of the property name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SettingDisplayNameAttribute : Observatory.Framework.SettingDisplayName
    {
        public SettingDisplayNameAttribute(string name) : base(name)
        { }
    }

    public class SettingDependsOnAttribute: Attribute
    {
        public string DependsOn { get; set; }

        public SettingDependsOnAttribute(string dependsOn)
        {
            DependsOn = dependsOn;
        }   
    }


    /// <summary>
    /// Indicates that the property should not be displayed to the user in the UI.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SettingIgnoreAttribute: Observatory.Framework.SettingIgnore
    { }

    /// <summary>
    /// Indicates numeric properly should use a slider control instead of a numeric textbox with roller.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SettingNumericUseSliderAttribute: Observatory.Framework.SettingNumericUseSlider
    { }

    /// <summary>
    /// Specify backing value used by Dictionary&lt;string, object&gt; to indicate selected option.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    [Obsolete("Use the SettingGetItemsMethodAttribute instead of this attribute")]
    public class SettingBackingValueAttribute : Observatory.Framework.SettingBackingValue
    {
        /// <summary>
        /// Specify backing value used by Dictionary&lt;string, object&gt; to indicate selected option.
        /// </summary>
        /// <param name="property">Property name for backing value.</param>
        public SettingBackingValueAttribute(string property) : base(property)
        {
        }
    }

    /// <summary>
    /// Instead of the SettingBackingValueAttribute, a method name on the Plugin can be provided that
    /// will return the Dictionary of values to be used to populate the drop down list. The
    /// Setting field is used to store the selected value. This attribute will always convert the 
    /// setting to a ComboBox selection. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SettingGetItemsMethodAttribute: Attribute
    {
        public SettingGetItemsMethodAttribute(string methodName)
        {
            this.MethodName = methodName;
        }

        public string MethodName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SettingPluginActionAttribute : Attribute
    {
        public SettingPluginActionAttribute(string methodName)
        {
            this.MethodName = methodName;
        }

        public string MethodName { get; set; }
    }

    /// <summary>
    /// Specify bounds for numeric inputs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SettingNumericBoundsAttribute: Observatory.Framework.SettingNumericBounds
    {
        /// <summary>
        /// Specify bounds for numeric inputs.
        /// </summary>
        /// <param name="minimum">Minimum allowed value.</param>
        /// <param name="maximum">Maximum allowed value.</param>
        /// <param name="increment">Increment between allowed values in slider/roller inputs.</param>
        public SettingNumericBoundsAttribute(double minimum, double maximum, double increment = 1.0)
            : base(minimum, maximum, increment)
        { }

    }
}
