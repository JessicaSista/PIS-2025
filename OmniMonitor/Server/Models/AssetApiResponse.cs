using System.Text.Json.Serialization;

using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Models
{
    public class AssetApiResponse
    {
        [JsonPropertyName("results")]
        public List<AssetDto> Results { get; set; } = new ();

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }
    }
}
