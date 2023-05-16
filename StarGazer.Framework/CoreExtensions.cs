using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using StarGazer.Framework.Interfaces;

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
                prop.SetValue(target, prop.GetValue(source, null));
            }
            return target;
        }
    }
}
