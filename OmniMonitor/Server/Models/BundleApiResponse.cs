using System.Collections.Generic;
using System.Text.Json.Serialization;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Models
{
    public class BundleApiResponse
    {
        [JsonPropertyName("results")]
        public List<BundleDto> Results { get; set; } = new();

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }
    }
}
