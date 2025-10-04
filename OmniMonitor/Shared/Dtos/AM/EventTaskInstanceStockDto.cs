namespace OmniMonitor.Shared.Dtos.AM
{
    public class EventTaskInstanceStockDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int Minimum { get; set; }
        public int BundleId { get; set; }
        public int UpdatedQuantity { get; set; }
        public List<object> ExtraInfoRemoved { get; set; } = new();
    }
}