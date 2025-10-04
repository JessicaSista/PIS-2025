namespace OmniMonitor.Shared.Dtos.EM
{
    public class CategoryGroupDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<CategoryDto>? Categories { get; set; }
        public List<int>? CategoryIds { get; set; }
    }
}
