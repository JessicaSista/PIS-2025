namespace OmniMonitor.Shared.Dtos.EM
{
    public class ExtensionDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("extensionId")]
        public int ExtensionId { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("extensionState")]
        public string ExtensionState { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("takenByUsername")]
        public string? TakenByUsername { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("takenByName")]
        public string? TakenByName { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("takenByLastName")]
        public string? TakenByLastName { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("eventDateTime")]
        public DateTime EventDateTime { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("lastModification")]
        public DateTime LastModification { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("categories")]
        public List<CategoryDto> Categories { get; set; } = new();
        [System.Text.Json.Serialization.JsonPropertyName("workZoneId")]
        public int WorkZoneId { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("workZoneName")]
        public string WorkZoneName { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("eventId")]
        public int EventId { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("eventName")]
        public string EventName { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("extensionSource")]
        public string? ExtensionSource { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("address")]
        public string? Address { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("location")]
        public LocationDto? Location { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("dangerous")]
        public bool Dangerous { get; set; }
    }

    public class EventTypeSchemaValueDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class SchemaValueDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
