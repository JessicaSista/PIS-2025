namespace OmniMonitor.Server.Configuration
{

    /// <summary>
    /// Represents the nested "BaseUrl" object in the JSON.
    /// </summary>
    public class BaseUrlConfig
    {
        public string? UrlIM { get; set; }

        public string? UrlAM { get; set; }

        public string? UrlEM { get; set; }

        public string? UrlUM { get; set; }
    }
}
