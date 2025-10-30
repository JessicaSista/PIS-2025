using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class KpiRequest
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("sourceModule")]
        public string? SourceModule { get; set; }

        [JsonPropertyName("datasetId")]
        public int? DatasetId { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }

        [JsonPropertyName("metric")]
        public string? Metric { get; set; }

        [JsonPropertyName("multiplier")]
        public double? Multiplier { get; set; }

        [JsonPropertyName("defaultColor")]
        public string? DefaultColor { get; set; }

        [JsonPropertyName("colorRanges")]
        public string? ColorRanges { get; set; }

        [JsonPropertyName("extraInfo")]
        public string? ExtraInfo { get; set; }
    }
}
