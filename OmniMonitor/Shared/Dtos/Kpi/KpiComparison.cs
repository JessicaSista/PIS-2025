namespace OmniMonitor.Shared.Dtos.Kpi
{
    /// <summary>
    /// Comparación del KPI con un período anterior
    /// </summary>
    public class KpiComparison
    {
        /// <summary>
        /// Valor del período anterior
        /// </summary>
        public object PreviousValue { get; set; } = 0;

        /// <summary>
        /// Valor formateado del período anterior
        /// </summary>
        public string FormattedPreviousValue { get; set; } = string.Empty;

        /// <summary>
        /// Diferencia absoluta (actual - anterior)
        /// </summary>
        public double AbsoluteDifference { get; set; }

        /// <summary>
        /// Diferencia porcentual
        /// </summary>
        public double PercentageDifference { get; set; }

        /// <summary>
        /// Diferencia formateada para mostrar
        /// </summary>
        public string FormattedDifference { get; set; } = string.Empty;

        /// <summary>
        /// Dirección del cambio: "up", "down", "same"
        /// </summary>
        public string Direction { get; set; } = "same";

        /// <summary>
        /// Período de comparación utilizado
        /// </summary>
        public string ComparisonPeriod { get; set; } = string.Empty;

        /// <summary>
        /// Fecha inicio del período anterior
        /// </summary>
        public DateTime PreviousPeriodStart { get; set; }

        /// <summary>
        /// Fecha fin del período anterior
        /// </summary>
        public DateTime PreviousPeriodEnd { get; set; }
    }
}