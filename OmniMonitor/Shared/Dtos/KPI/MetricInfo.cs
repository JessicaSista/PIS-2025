using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos;
public class MetricInfo
{

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("extraInfo")]
    public string? ExtraInfo { get; set; }


}
