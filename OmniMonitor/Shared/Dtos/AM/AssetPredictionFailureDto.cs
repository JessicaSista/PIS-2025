using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using OmniMonitor.Shared.Dtos.AM;

namespace OmniMonitor.Shared.Dtos
{
    public class AssetPredictionFailureDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("prediction")]
        public int Prediction { get; set; }
        [JsonPropertyName("probabilityOfBeing0")]
        public double ProbabilityOfBeing0 { get; set; }
        [JsonPropertyName("probabilityOfbeing1")]
        public double ProbabilityOfbeing1 { get; set; }
        [JsonPropertyName("taskInstance")]
        public EventTaskInstanceDto? TaskInstance { get; set; }
        [JsonPropertyName("assetId")]
        public int AssetId { get; set; }
        [JsonPropertyName("asset")]
        public object? Asset { get; set; }
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }
        [JsonPropertyName("history")]
        public List<AssetPredictionFailureHistoryDto>? History { get; set; }
    }
}