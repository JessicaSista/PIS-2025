using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class DeviceReduced
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("connectionString")]
        public string? ConnectionString { get; set; }

        [JsonPropertyName("groups")]
        public string? Groups { get; set; }

        [JsonPropertyName("sensors")]
        public string? Sensors { get; set; }
    }
}