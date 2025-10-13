using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos.Kpi
{
    /// <summary>
    /// Request para calcular KPIs desde un dataset específico
    /// </summary>
    public class CalculateKpiRequest
    {
        /// <summary>
        /// ID del dataset a utilizar para el cálculo
        /// </summary>
        [Required]
        public int DatasetId { get; set; }

        /// <summary>
        /// Tipo de dataset: "IM", "EM", "UM"
        /// </summary>
        [Required]
        [MaxLength(2)]
        public string DatasetType { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de métrica a calcular: "count", "sum", "average", "min", "max"
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string MetricType { get; set; } = string.Empty;

        /// <summary>
        /// Fecha inicio del rango para el cálculo
        /// </summary>
        [Required]
        public DateTime DateFrom { get; set; }

        /// <summary>
        /// Fecha fin del rango para el cálculo
        /// </summary>
        [Required]
        public DateTime DateTo { get; set; }

        /// <summary>
        /// Campo específico sobre el cual calcular la métrica (ej: "temperature", "humidity")
        /// </summary>
        public string? FieldName { get; set; }

        /// <summary>
        /// Agrupación temporal: "hour", "day", "week", "month"
        /// </summary>
        public string? GroupBy { get; set; }

        /// <summary>
        /// Filtros adicionales específicos por tipo de dataset
        /// </summary>
        public Dictionary<string, object>? Filters { get; set; }

        /// <summary>
        /// Formato de salida: "number", "percentage", "currency", "time"
        /// </summary>
        public string? FormatType { get; set; } = "number";

        /// <summary>
        /// Incluir comparación con período anterior
        /// </summary>
        public bool IncludeComparison { get; set; } = false;

        /// <summary>
        /// Incluir datos de tendencia para sparkline
        /// </summary>
        public bool IncludeTrend { get; set; } = false;

        /// <summary>
        /// Umbrales para determinar el estado del KPI
        /// </summary>
        public KpiThresholds? Thresholds { get; set; }
    }
}