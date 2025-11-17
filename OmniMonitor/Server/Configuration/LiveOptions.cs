namespace OmniMonitor.Server.Configuration
{
    public class LiveOptions
    {
        public int ImChartIntervalSeconds { get; set; } = 3;
        public int ImKpiIntervalSeconds { get; set; } = 5;
        public int EmKpiIntervalSeconds { get; set; } = 15;
        public BackoffOptions Backoff { get; set; } = new BackoffOptions();
    }

    public class BackoffOptions
    {
        public int BaseSeconds { get; set; } = 5;
        public int MaxSeconds { get; set; } = 60;
        public double Multiplier { get; set; } = 2.0;
        public double JitterRatio { get; set; } = 0.2;
    }
}
