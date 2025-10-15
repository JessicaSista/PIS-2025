using System;
using System.Collections.Generic;

namespace OmniMonitor.Shared.Dtos.KpiDtos
{
    /// <summary>
    /// Respuesta del cálculo de KPI
    /// </summary>
    public class KpiResponse
    {
        public object Value { get; set; } = 0;
        public string FormattedValue { get; set; } = string.Empty;
        public string FormatType { get; set; } = "number";
        public string? Unit { get; set; }
        public KpiComparison? Comparison { get; set; }
        public List<KpiTrendPoint>? Trend { get; set; }
        public string Status { get; set; } = "unknown";
        public string? StatusMessage { get; set; }
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan CalculationTime { get; set; }
        public KpiDatasetInfo DatasetInfo { get; set; } = new();
        public bool FromCache { get; set; } = false;
        public string MetricType { get; set; } = string.Empty;
        public string? FieldName { get; set; }
    }
}
