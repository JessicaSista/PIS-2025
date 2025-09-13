using System.Collections.Generic;

namespace OmniMonitor.Server.Configuration
{
    public class ApiConfig
    {
        public string BaseUrl { get; set; }
        public Credentials Credentials { get; set; }
        public Dictionary<string, Dictionary<string, string>> Endpoints { get; set; }
    }

    public class Credentials
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}