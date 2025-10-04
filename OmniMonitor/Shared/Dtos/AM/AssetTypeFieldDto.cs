using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class AssetTypeFieldDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; } // nullable

        [JsonPropertyName("type")]
        public string? Type { get; set; } // nullable

        [JsonPropertyName("value")]
        public string? Value { get; set; } // nullable

        [JsonPropertyName("usedInActive")]
        public bool UsedInActive { get; set; }
    }
}
