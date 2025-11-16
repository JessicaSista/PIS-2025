using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class SourceReduced
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("active")]
        public string? Active { get; set; }

        [JsonPropertyName("devices")]
        public string? Devices { get; set; }

        [JsonPropertyName("sensors")]
        public string? Sensors { get; set; }
    }
}