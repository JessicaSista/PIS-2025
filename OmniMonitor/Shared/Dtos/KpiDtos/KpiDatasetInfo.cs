namespace OmniMonitor.Shared.Dtos.KpiDtos
{
    /// <summary>
    /// Información sobre el dataset utilizado para el cálculo del KPI
    /// </summary>
    public class KpiDatasetInfo
    {
        public int DatasetId { get; set; }
        public string DatasetName { get; set; } = string.Empty;
        public string DatasetType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Username { get; set; }
        public int TotalRecords { get; set; }
    }
}
