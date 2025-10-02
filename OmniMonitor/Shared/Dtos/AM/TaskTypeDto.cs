using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos.AM
{
    public class TaskTypeDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        [JsonPropertyName("extraFields")]
        public string? ExtraFields { get; set; }
        [JsonPropertyName("conditionalTaskType")]
        public bool ConditionalTaskType { get; set; }
        [JsonPropertyName("aprobation")]
        public bool Aprobation { get; set; }
        [JsonPropertyName("category")]
        public int Category { get; set; }
        [JsonPropertyName("taskTypeConditionDto")]
        public TaskTypeConditionsDto? TaskTypeConditionDto { get; set; }
    }
}