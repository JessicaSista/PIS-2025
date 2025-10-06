using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class LocationDto
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("radius")]
        public double? Radius { get; set; }
    }
}
