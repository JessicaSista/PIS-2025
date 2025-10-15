using System;

namespace OmniMonitor.Shared.Dtos.KpiDtos
{
    /// <summary>
    /// Comparación del KPI con un período anterior
    /// </summary>
    public class KpiComparison
    {
        public object PreviousValue { get; set; } = 0;
        public string FormattedPreviousValue { get; set; } = string.Empty;
        public double AbsoluteDifference { get; set; }
        public double PercentageDifference { get; set; }
        public string FormattedDifference { get; set; } = string.Empty;
        public string Direction { get; set; } = "same";
        public string ComparisonPeriod { get; set; } = string.Empty;
        public DateTime PreviousPeriodStart { get; set; }
        public DateTime PreviousPeriodEnd { get; set; }
    }
}
