using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos;
public class KpiResponse
{

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("actualColor")]
    public string? ActualColor { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    [JsonPropertyName("unit")]
    public object? Unit { get; set; }

    [JsonPropertyName("datasetName")]
    public string? DatasetName { get; set; }

}
