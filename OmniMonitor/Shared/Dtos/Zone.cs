using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OmniMonitor.Shared.Dtos
{
    public class Zone
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty; 

        [JsonPropertyName("reference")]
        public string? Reference { get; set; }

        [JsonPropertyName("areas")]
        public List<string>? Areas { get; set; }
    }
}
