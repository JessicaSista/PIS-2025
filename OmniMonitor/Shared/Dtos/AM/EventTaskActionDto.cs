namespace OmniMonitor.Shared.Dtos.AM
{
    public class EventTaskActionDto
    {
        public int Id { get; set; }
        public bool Active { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Completed { get; set; }
        public int Value { get; set; }
    }
}