using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OmniMonitor.Shared.Dtos;
public class DeviceAction
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("deviceId")]
    public int DeviceId { get; set; }

    [JsonPropertyName("actionId")]
    public int? ActionId { get; set; }

    [JsonPropertyName("action")]
    public SondaAction? Action { get; set; }

    [JsonPropertyName("messageType")]
    public string? MessageType { get; set; }

    [JsonPropertyName("messageStructure")]
    public string? MessageStructure { get; set; }

    [JsonPropertyName("active")]
    public int? Active { get; set; }

    // 'actionExecutions' can be defined with a specific type if its structure is known
    [JsonPropertyName("actionExecutions")]
    public List<object>? ActionExecutions { get; set; }
}
