using System;

namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Entidad KPI para persistencia en base de datos
    /// </summary>
    public class Kpi
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MetricType { get; set; } = string.Empty;
        public string FormatType { get; set; } = "number";
        public string? Unit { get; set; }
        public double Value { get; set; }
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
        public string Username { get; set; } = string.Empty;
    }
}
