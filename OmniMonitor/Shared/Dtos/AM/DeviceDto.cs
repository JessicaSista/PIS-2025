using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class DeviceDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; } // nullable

        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; } // nullable

        [JsonPropertyName("source")]
        public SourceDto? Source { get; set; }
    }
}
