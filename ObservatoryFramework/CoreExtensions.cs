using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Observatory.Framework.Interfaces;

namespace Observatory.Framework
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

        public static object CopyAs(this object source, Type targetType)
        {
            string json = JsonSerializer.Serialize(source, SerializerOptions);
            var instance = JsonSerializer.Deserialize(json, targetType, SerializerOptions);
            return instance;
        }

        public static T CopyAs<T>(this object source) where T : notnull
        {
            return (T)CopyAs(source, typeof(T));
        }
    }
}
