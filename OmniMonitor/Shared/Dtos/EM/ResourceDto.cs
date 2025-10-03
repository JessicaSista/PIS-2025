namespace OmniMonitor.Shared.Dtos.EM
{
    public class ResourceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ExternalId { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int IconFreeId { get; set; }
        public IconDto? IconFree { get; set; }
        public int IconNotFreeId { get; set; }
        public IconDto? IconNotFree { get; set; }
        public int IconDisabledId { get; set; }
        public IconDto? IconDisabled { get; set; }
        public int IconArrivedId { get; set; }
        public IconDto? IconArrived { get; set; }
        public object? ContactMethod { get; set; }
        public LocationDto? Location { get; set; }
        public int UserCount { get; set; }
        public WorkZoneDto? CurrentWorkZone { get; set; }
        public List<string>? CurrentUsersEmail { get; set; }
        public List<string>? UsersEmail { get; set; }
        public List<int>? WorkZoneIds { get; set; }
        public ResourceSimpleDto? ResourceType { get; set; }
    }

    public class IconDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IconData { get; set; } = string.Empty;
        public int Type { get; set; }
    }

    public class ResourceSimpleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<object>? Schema { get; set; }
    }
}
