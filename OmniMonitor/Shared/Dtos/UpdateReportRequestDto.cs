using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    public class UpdateReportRequestDto
    {
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public string? JSON_config { get; set; }
        public ReportFiltersConfig? Filters { get; set; }
    }
}