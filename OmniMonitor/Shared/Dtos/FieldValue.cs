using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class FieldValue
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; } // nullable

        [JsonPropertyName("type")]
        public string? Type { get; set; } // nullable

        [JsonPropertyName("value")]
        public string? Value { get; set; } // nullable
    }
}
