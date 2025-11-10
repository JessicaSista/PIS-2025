namespace OmniMonitor.Server.Configuration
{
    /// <summary>
    /// This is the main configuration class that maps to your entire JSON file.
    /// </summary>
    public class ApiConfig
    {
        public BaseUrlConfig? BaseUrl { get; set; }

        public CredentialsConfig? Credentials { get; set; }

        public Dictionary<string, Dictionary<string, string>>? EndpointsIM { get; set; }

        public Dictionary<string, Dictionary<string, string>>? EndpointsAM { get; set; }

        public Dictionary<string, Dictionary<string, string>>? EndpointsEM { get; set; }

        public Dictionary<string, Dictionary<string, string>>? EndpointsUM { get; set; }
    }
}
