using System.Collections.Generic;

namespace OmniMonitor.Server.Configuration
{
    /// <summary>
    /// This is the main configuration class that maps to your entire JSON file.
    /// </summary>
    public class ApiConfig
    {
        public BaseUrlConfig BaseUrl { get; set; }
        public CredentialsConfig Credentials { get; set; }
        public Dictionary<string, Dictionary<string, string>> EndpointsIM { get; set; }
        public Dictionary<string, Dictionary<string, string>> EndpointsAM { get; set; }
        public Dictionary<string, Dictionary<string, string>> EndpointsEM { get; set; }
        public Dictionary<string, Dictionary<string, string>> EndpointsUM { get; set; }
    }

    /// <summary>
    /// Represents the nested "BaseUrl" object in the JSON.
    /// </summary>
    public class BaseUrlConfig
    {
        public string UrlIM { get; set; }
        public string UrlAM { get; set; }
        public string UrlEM { get; set; }
        public string UrlUM { get; set; }
    }

    /// <summary>
    /// Represents the nested "Credentials" object, which contains multiple credential sets.
    /// </summary>
    public class CredentialsConfig
    {
        public CredentialDetails CredentialsIM { get; set; }
        public CredentialDetails CredentialsAM { get; set; }
        public CredentialDetails CredentialsEM { get; set; }
        public CredentialDetails CredentialsUM { get; set; }
    }

    /// <summary>
    /// Represents a single set of credentials (Email and Password).
    /// </summary>
    public class CredentialDetails
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}