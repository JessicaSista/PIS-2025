namespace OmniMonitor.Shared.Dtos.EM
{
    public class CategoryDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("CategoryId")]
        public int CategoryId { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("CategoryState")]
        public string CategoryState { get; set; } = string.Empty;
        public int Id { get; set; }
        public bool Active { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = string.Empty;
        public List<CategoryGroupDto>? Groups { get; set; }
        public List<int>? GroupIds { get; set; }
        public string? Protocol { get; set; }
    public TypeEventCategory? TypeCategoryEvent { get; set; }
        public bool Selected { get; set; }
        public List<object>? ActionsDtos { get; set; }
        public List<object>? WorkZones { get; set; }
    }

    public enum TypeEventCategory
    {
        Type0 = 0,
        Type1 = 1
    }
}
