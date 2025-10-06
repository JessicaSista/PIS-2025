using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class EventField
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty; // obligatorio, minLength = 1

        [JsonPropertyName("fieldType")]
        public string FieldType { get; set; } = string.Empty; // obligatorio, minLength = 1

        [JsonPropertyName("defaultValue")]
        public string? DefaultValue { get; set; } // nullable
    }
}
