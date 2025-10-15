using System;

namespace OmniMonitor.Shared.Dtos.KpiDtos
{
    /// <summary>
    /// Punto de datos para la tendencia del KPI (sparkline)
    /// </summary>
    public class KpiTrendPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }
}
