using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Observatory.Framework.Files.ParameterTypes;

namespace Observatory.Framework.Files.Converters
{
    public class StationServiceConverter : JsonConverter<StationService>
    {
        public override StationService Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            StationService services = StationService.None;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                string value = reader.GetString();
                services |= (StationService)Enum.Parse(typeof(StationService), value, true);
            }

            return services;

        }

        public override void Write(Utf8JsonWriter writer, StationService value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
