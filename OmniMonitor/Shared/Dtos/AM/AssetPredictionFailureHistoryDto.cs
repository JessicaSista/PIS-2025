using System;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class AssetPredictionFailureHistoryDto
    {
        [JsonPropertyName("prediction")]
        public int Prediction { get; set; }
        [JsonPropertyName("probabilityOfBeing0")]
        public double ProbabilityOfBeing0 { get; set; }
        [JsonPropertyName("proabbilityOfbeing1")]
        public double ProabbilityOfbeing1 { get; set; }
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }
        [JsonPropertyName("taskInstanceId")]
        public int? TaskInstanceId { get; set; }
    }
}