using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class SensorReduced
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("integration")]
        public string? Integration { get; set; }

        [JsonPropertyName("lastValue")]
        public string? LastValue { get; set; }
    }
}