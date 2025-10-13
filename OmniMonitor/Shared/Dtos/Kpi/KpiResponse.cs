namespace OmniMonitor.Shared.Dtos.Kpi
{
    /// <summary>
    /// Respuesta del cálculo de KPI
    /// </summary>
    public class KpiResponse
    {
        /// <summary>
        /// Valor principal calculado del KPI
        /// </summary>
        public object Value { get; set; } = 0;

        /// <summary>
        /// Valor formateado para mostrar al usuario
        /// </summary>
        public string FormattedValue { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de formato aplicado
        /// </summary>
        public string FormatType { get; set; } = "number";

        /// <summary>
        /// Unidad de medida del valor
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// Tipo de métrica calculada
        /// </summary>
        public string MetricType { get; set; } = string.Empty;

        /// <summary>
        /// Campo sobre el cual se calculó la métrica
        /// </summary>
        public string? FieldName { get; set; }

        /// <summary>
        /// Comparación con período anterior (si se solicitó)
        /// </summary>
        public KpiComparison? Comparison { get; set; }

        /// <summary>
        /// Datos de tendencia para sparkline (si se solicitó)
        /// </summary>
        public List<KpiTrendPoint>? Trend { get; set; }

        /// <summary>
        /// Estado del KPI basado en umbrales: "ok", "warning", "critical", "unknown"
        /// </summary>
        public string Status { get; set; } = "unknown";

        /// <summary>
        /// Mensaje explicativo del estado
        /// </summary>
        public string? StatusMessage { get; set; }

        /// <summary>
        /// Momento en que se calculó el KPI
        /// </summary>
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Tiempo que tomó calcular el KPI
        /// </summary>
        public TimeSpan CalculationTime { get; set; }

        /// <summary>
        /// Información adicional sobre el dataset utilizado
        /// </summary>
        public KpiDatasetInfo DatasetInfo { get; set; } = new();

        /// <summary>
        /// Indica si el cálculo proviene de caché
        /// </summary>
        public bool FromCache { get; set; } = false;
    }
}