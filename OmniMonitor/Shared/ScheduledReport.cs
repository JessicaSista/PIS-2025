using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniMonitor.Shared
{
    public class ScheduledReport
    {
        public int Id { get; set; }

        public int ReportId { get; set; }
        public string Username { get; set; }

        public string ScheduleType { get; set; }
        public int? IntervalMinutes { get; set; }
        public string? SendAtLocalTime { get; set; }
        public string? AdvancedRule { get; set; }
        public string TimeZone { get; set; }

        public string RecipientsJson { get; set; }

        public string Subject { get; set; }
        public string Message { get; set; }

        public DateTime? LastExecution { get; set; }
        public bool IsActive { get; set; } = true;
    }

}
