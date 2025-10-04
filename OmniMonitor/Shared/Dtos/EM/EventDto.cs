namespace OmniMonitor.Shared.Dtos.EM
{
    public class EventDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastModification { get; set; }
        public int SourceType { get; set; }
        public string State { get; set; } = string.Empty;
        public AddressDto? Address { get; set; }
        public EMLocationDto? Location { get; set; }
        public List<EventCategoryDto> Categories { get; set; } = new();
    }

    public class AddressDto
    {
        public int PlaceId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class EventCategoryDto
    {
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public int Id { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("active")]
    public bool Active { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;
    //[System.Text.Json.Serialization.JsonPropertyName("groups")]
    //public List<object> Groups { get; set; } = new();
    [System.Text.Json.Serialization.JsonPropertyName("groupIds")]
    public List<int> GroupIds { get; set; } = new();
    [System.Text.Json.Serialization.JsonPropertyName("typeCategoryEvent")]
    public int TypeCategoryEvent { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("selected")]
    public bool Selected { get; set; }
    //[System.Text.Json.Serialization.JsonPropertyName("workZones")]
    //public List<object> WorkZones { get; set; } = new();
    }
}