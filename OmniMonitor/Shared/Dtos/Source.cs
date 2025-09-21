using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OmniMonitor.Shared.Dtos;
public class Source
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("timeTolerance")]
    public int? TimeTolerance { get; set; }

    [JsonPropertyName("timeRange")]
    public int? TimeRange { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("tenantId")]
    public int? TenantId { get; set; }

    [JsonPropertyName("noDataAlert")]
    public bool NoDataAlert { get; set; }

    [JsonPropertyName("noDataSleepByDevice")]
    public int? NoDataSleepByDevice { get; set; }

    [JsonPropertyName("noDataInterval")]
    public int? NoDataInterval { get; set; }

    [JsonPropertyName("outputId")]
    public int? OutputId { get; set; }

    [JsonPropertyName("devices")]
    public List<Device>? Devices { get; set; }

    [JsonPropertyName("sensors")]
    public List<Sensor>? Sensors { get; set; }

}