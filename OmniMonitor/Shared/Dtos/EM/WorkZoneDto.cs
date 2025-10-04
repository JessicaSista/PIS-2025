namespace OmniMonitor.Shared.Dtos.EM
{
    public class WorkZoneDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CreationDate { get; set; } = string.Empty;
        public string LastModification { get; set; } = string.Empty;
        public bool Selected { get; set; }
        public List<PlaneDto>? Planes { get; set; }
        public List<int>? DeletedPlanes { get; set; }
        public List<string>? UserIds { get; set; }
        public List<EMUserDto>? Users { get; set; }
        public int UsersCount { get; set; }
        public List<int>? CurrentResources { get; set; }
        public List<int>? Resources { get; set; }
    }
}
