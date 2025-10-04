using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos.AM
{
    public class ConditionalLastRegisteredValueDto
    {
        [JsonPropertyName("assetId")]
        public string? AssetId { get; set; }
        [JsonPropertyName("assetName")]
        public string? AssetName { get; set; }
        [JsonPropertyName("lastRegisteredValue")]
        public string? LastRegisteredValue { get; set; }
    }
}