namespace OmniMonitor.Shared.Dtos.Kpi
{
    /// <summary>
    /// Punto de datos para la tendencia del KPI (sparkline)
    /// </summary>
    public class KpiTrendPoint
    {
        /// <summary>
        /// Timestamp del punto de datos
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Valor del KPI en este punto temporal
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Etiqueta para mostrar en el eje X
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Valor formateado para tooltip
        /// </summary>
        public string FormattedValue { get; set; } = string.Empty;
    }
}