using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class Zone
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty; 

        [JsonPropertyName("reference")]
        public string? Reference { get; set; }

        [JsonPropertyName("areas")]
        public List<string>? Areas { get; set; }

        public override string ToString() => Name;
    }
}
