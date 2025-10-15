namespace OmniMonitor.Shared.Dtos.KpiDtos
{
    /// <summary>
    /// Define los umbrales para determinar el estado de un KPI
    /// </summary>
    public class KpiThresholds
    {
        public double? Ok { get; set; }
        public double? Warning { get; set; }
        public double? Critical { get; set; }
    }
}
