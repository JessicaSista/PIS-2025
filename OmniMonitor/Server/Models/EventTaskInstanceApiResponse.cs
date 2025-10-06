using OmniMonitor.Shared.Dtos.AM;
using System.Collections.Generic;

namespace OmniMonitor.Server.Models
{
    public class EventTaskInstanceApiResponse
    {
        public List<EventTaskInstanceDto> Results { get; set; }
        public string ErrorMessage { get; set; }
        public int TotalItems { get; set; }
    }
}
