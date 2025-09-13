using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OmniMonitor.Shared.Dtos;
    public class Integration
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; set; }
}
