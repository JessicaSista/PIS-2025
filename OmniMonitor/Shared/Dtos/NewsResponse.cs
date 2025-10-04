using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class NewsResponse
    {
        [JsonPropertyName("results")]
        public List<News> results { get; set; } = new List<News>();

        [JsonPropertyName("errorMessage")]
        public string errorMessage { get; set; } = string.Empty;

        [JsonPropertyName("totalItems")]
        public int totalItems { get; set; }
    }
}
