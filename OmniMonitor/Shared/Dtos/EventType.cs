using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class EventType
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty; // obligatorio, minLength = 1

        [JsonPropertyName("description")]
        public string? Description { get; set; } // nullable

        [JsonPropertyName("fields")]
        public List<EventField>? Fields { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("requiresApproval")]
        public bool RequiresApproval { get; set; }

        [JsonPropertyName("ratingType")]
        public string? RatingType { get; set; }

        [JsonPropertyName("relevant")]
        public bool Relevant { get; set; }

        [JsonPropertyName("sendToExternal")]
        public bool SendToExternal { get; set; }

        [JsonPropertyName("showInList")]
        public bool ShowInList { get; set; }
    }
}
