namespace OmniMonitor.Shared.Dtos.EM
{
    public class EventApiResponse
    {
        public List<EventDto> Results { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
        public int CurrentPage { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }
}
