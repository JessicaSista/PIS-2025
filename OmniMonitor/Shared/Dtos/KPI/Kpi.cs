using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class Kpi
    {
        [Key]
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }


        [JsonPropertyName("sourceModule")]
        public string SourceModule { get; set; } = string.Empty;

        [JsonPropertyName("datasetId")]
        public int DatasetId { get; set; }

        // Navigation property to Datasets
        public virtual Datasets? Dataset { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }

        [JsonPropertyName("metric")]
        public string? Metric { get; set; }

        [JsonPropertyName("multiplier")]
        public double? Multiplier { get; set; }

        [JsonPropertyName("defaultColor")]
        public string? DefaultColor{ get; set; }

        [JsonPropertyName("colorRanges")]
        public string? ColorRanges { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("atributo")]   
        public string Atributo { get; set; }

        [JsonPropertyName("extraInfo")]
        public string? ExtraInfo { get; set; }

        [JsonPropertyName("type")]
        public int? Type { get; set; }  

        [JsonPropertyName("liveEnabled")]
        public bool LiveEnabled { get; set; } = false;

    }


}
