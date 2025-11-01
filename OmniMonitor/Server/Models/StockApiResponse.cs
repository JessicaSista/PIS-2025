using System.Collections.Generic;
using System.Text.Json.Serialization;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;

namespace OmniMonitor.Server.Models
{
    public class StockApiResponse
    {
        [JsonPropertyName("results")]
        public List<StockDto> Results { get; set; } = new ();

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }
    }
}
