using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos.EM
{
    public class ExtensionDtoDup
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;
        [JsonPropertyName("takenBy")]
        public EMUserDto? TakenBy { get; set; }
        [JsonPropertyName("createdBy")]
        public EMUserDto? CreatedBy { get; set; }
        [JsonPropertyName("eventDateTime")]
        public DateTime EventDateTime { get; set; }
        [JsonPropertyName("creationDate")]
        public DateTime CreationDate { get; set; }
        [JsonPropertyName("lastModification")]
        public DateTime LastModification { get; set; }
        [JsonPropertyName("categories")]
        public List<CategoryDto> Categories { get; set; } = new();
        [JsonPropertyName("workZoneId")]
        public int WorkZoneId { get; set; }
        [JsonPropertyName("workZoneName")]
        public string WorkZoneName { get; set; } = string.Empty;
        [JsonPropertyName("userIsInWorkZone")]
        public bool UserIsInWorkZone { get; set; }
        [JsonPropertyName("eventId")]
        public int EventId { get; set; }
        [JsonPropertyName("eventName")]
        public string EventName { get; set; } = string.Empty;
        [JsonPropertyName("eventOrigin")]
        public string EventOrigin { get; set; } = string.Empty;
        [JsonPropertyName("address")]
        public AddressDto? Address { get; set; }
        [JsonPropertyName("sourceType")]
        public int SourceType { get; set; }
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
        [JsonPropertyName("eventTypeSchemaValues")]
        public List<EventTypeSchemaValueDto> EventTypeSchemaValues { get; set; } = new();
        [JsonPropertyName("workZoneSchemaFields")]
        public List<SchemaFieldDto> WorkZoneSchemaFields { get; set; } = new();
        [JsonPropertyName("workZoneSchemaValues")]
        public List<SchemaValueDto> WorkZoneSchemaValues { get; set; } = new();
        [JsonPropertyName("dangerous")]
        public bool Dangerous { get; set; }
        public int Count { get; set; }
    }
}
