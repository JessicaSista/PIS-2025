using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniMonitor.Shared.Dtos
{
    public class ScheduledReportRequest
    {
        public int ReportId { get; set; }

        // "Daily", "Weekly", "Monthly", "Advanced"
        public string ScheduleType { get; set; }

        // Interval-based scheduling (optional)
        public int? IntervalMinutes { get; set; }

        // Fixed-time scheduling (optional)
        public string? SendAtLocalTime { get; set; }

        // Used only for advanced rules (e.g. "Mon,Thu 08:00")
        public string? AdvancedRule { get; set; }

        public string TimeZone { get; set; }

        public List<string> Recipients { get; set; }

        public string Subject { get; set; }

        public string Message { get; set; }
    }


}
