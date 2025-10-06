namespace OmniMonitor.Shared.Dtos.EM
{
    public class EventTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool AutoArchive { get; set; }
        public string? DefaultName { get; set; }
        public string? DefaultOrigin { get; set; }
        public List<TemplateDto>? Templates { get; set; }
        public List<SchemaFieldDto>? Schema { get; set; }
        public string? SendToExternal { get; set; }
        public List<CategoryDto>? DefaultCategories { get; set; }
        public List<int>? DefaultCategoriesIds { get; set; }
        public List<WorkZoneDto>? DefaultWorkZones { get; set; }
        public List<int>? DefaultWorkZonesIds { get; set; }
        public int RequiredFiles { get; set; }
    }

}
