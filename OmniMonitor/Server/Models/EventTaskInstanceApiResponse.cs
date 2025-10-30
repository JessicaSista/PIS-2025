using OmniMonitor.Shared.Dtos.AM;

namespace OmniMonitor.Server.Models
{
    public class EventTaskInstanceApiResponse
    {
        public List<EventTaskInstanceDto> Results { get; set; } = new ();

        public string? ErrorMessage { get; set; }

        public int TotalItems { get; set; }
    }
}
