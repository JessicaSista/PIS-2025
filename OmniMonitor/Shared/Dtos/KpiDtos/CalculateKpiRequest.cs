namespace OmniMonitor.Shared.Dtos.KpiDtos
{
    /// <summary>
    /// Request para calcular KPIs desde un dataset específico
    /// </summary>
    public class CalculateKpiRequest
    {
        public int DatasetId { get; set; }
        public string MetricType { get; set; } = string.Empty;
        public string FormatType { get; set; } = "number";
        public string? Unit { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string DatasetType { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public DateTime DateFrom { get; set; } = DateTime.Now.AddDays(-7);
        public DateTime DateTo { get; set; } = DateTime.Now;
        // Puedes agregar más campos según lo que necesites para el cálculo
    }
}
