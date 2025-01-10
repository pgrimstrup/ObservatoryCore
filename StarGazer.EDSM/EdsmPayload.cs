using System.Text.Json.Serialization;

namespace StarGazer.EDSM
{
    public class EdsmPayload
    {
        [JsonIgnore]
        public DateTime Timestamp { get; set; }

        [JsonIgnore]
        public string? Event { get; set; }

        [JsonPropertyName("commanderName")]
        public string? CommanderName { get; set; }

        [JsonPropertyName("apiKey")]
        public string? ApiKey { get; set; }

        [JsonPropertyName("fromSoftware")]
        public string? FromSoftware { get; set; }

        [JsonPropertyName("fromSoftwareVersion")]
        public string? FromSoftwareVersion { get; set; }

        [JsonPropertyName("fromGameVersion")]
        public string? FromGameVersion { get; set; }

        [JsonPropertyName("fromGameBuild")]
        public string? FromGameBuild { get; set; }
  
        [JsonPropertyName("message")]
        public object? Message { get; set; }

        [JsonIgnore]
        public int Response { get; set; }
    }
}
