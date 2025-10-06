using System;


namespace OmniMonitor.Shared.Dtos.AM

{
    public class EventTaskDto
    {
        public int Id { get; set; }
        public bool Active { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public TaskTypeDto TypeDto { get; set; }
        public int TaskTypeId { get; set; }

        public string Ticket { get; set; }
        public UserDto Author { get; set; }
        public string Duration { get; set; }
        public string AlertsType { get; set; }
        public string AlertTimeSpan { get; set; }
        public bool PredictionTask { get; set; }
        public int BundleId { get; set; }
    }
}
