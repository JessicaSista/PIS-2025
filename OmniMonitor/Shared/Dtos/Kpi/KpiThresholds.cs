namespace OmniMonitor.Shared.Dtos.Kpi
{
    /// <summary>
    /// Define los umbrales para determinar el estado de un KPI
    /// </summary>
    public class KpiThresholds
    {
        /// <summary>
        /// Valor mínimo para considerarse en estado de advertencia
        /// </summary>
        public double? WarningMin { get; set; }

        /// <summary>
        /// Valor máximo para considerarse en estado de advertencia
        /// </summary>
        public double? WarningMax { get; set; }

        /// <summary>
        /// Valor mínimo para considerarse en estado crítico
        /// </summary>
        public double? CriticalMin { get; set; }

        /// <summary>
        /// Valor máximo para considerarse en estado crítico
        /// </summary>
        public double? CriticalMax { get; set; }

        /// <summary>
        /// Operador de comparación: ">", "<", "between", "="
        /// </summary>
        public string ComparisonOperator { get; set; } = "between";

        /// <summary>
        /// Invierte la lógica de comparación (para casos donde valores más altos son peores)
        /// </summary>
        public bool InvertLogic { get; set; } = false;
    }
}