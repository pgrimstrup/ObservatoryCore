using System.Text.Json.Serialization;
using Observatory.Framework.Files.Converters;
using Observatory.Framework.Files.ParameterTypes;

namespace Observatory.Framework.Files.Journal
{
    public class NavRoute : JournalBase
    {
        [JsonPropertyName("Route")]
        public Route[] Route { get; set; }
    }
}
