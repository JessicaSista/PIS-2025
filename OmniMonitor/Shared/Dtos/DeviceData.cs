using System;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class DeviceData
    {
        [JsonPropertyName("deviceId")]
        public int DeviceId { get; set; }

        [JsonPropertyName("sensor")]
        public string Sensor { get; set; }

        [JsonPropertyName("sensorDisplayName")]
        public string SensorDisplayName { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("timeStamp")]
        public DateTime TimeStamp { get; set; }

        [JsonPropertyName("data")]
        public string Data { get; set; }
    }
}