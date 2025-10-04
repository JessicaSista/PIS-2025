using System;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class SensorData
    {
        [JsonPropertyName("data")]
        public decimal Data { get; set; }

        [JsonPropertyName("time")]
        public DateTime Time { get; set; }
    }
}