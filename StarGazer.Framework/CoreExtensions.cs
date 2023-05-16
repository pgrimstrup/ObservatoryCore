using System.Text.Json;

namespace StarGazer.Framework
{
    public static class CoreExtensions
    {
        public static JsonSerializerOptions SerializerOptions;

        static CoreExtensions()
        {
            SerializerOptions = new JsonSerializerOptions();
            SerializerOptions.WriteIndented = true;
            SerializerOptions.IgnoreReadOnlyProperties = true;
            SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never;
            SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        }

        public static string SafeTrim(this string value, params char[] chars)
        {
            if (value == null)
                return null;

            if (chars == null || chars.Length == 0)
                return value.Trim();

            return value.Trim(chars);
        }

        public static object SimpleClone(this object source)
        {
            if (source == null)
                return null;

            var target = Activator.CreateInstance(source.GetType());
            foreach(var prop in source.GetType().GetProperties())
            {
                if(prop.CanRead && prop.CanWrite)
                    prop.SetValue(target, prop.GetValue(source, null));
            }
            return target;
        }
    }
}
