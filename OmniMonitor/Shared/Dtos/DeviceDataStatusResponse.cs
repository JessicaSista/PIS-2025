using System;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class DeviceDataStatusResponse
    {
        [JsonPropertyName("lastMigration")]
        public DateTime LastMigration { get; set; }

        [JsonPropertyName("countDeviceData")]
        public int CountDeviceData { get; set; }

        [JsonPropertyName("countHistoricDeviceData")]
        public int CountHistoricDeviceData { get; set; }

        [JsonPropertyName("lastCount")]
        public DateTime LastCount { get; set; }
    }
}
