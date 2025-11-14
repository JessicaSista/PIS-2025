namespace OmniMonitor.Shared.Dtos.EM
{
    public class AlertCategoryDto
    {
        public int Id { get; set; }
        public bool Active { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<int> groupIds { get; set; } = new();
        public int typeCategoryEvent { get; set; }
        public bool selected { get; set; }
    }
}