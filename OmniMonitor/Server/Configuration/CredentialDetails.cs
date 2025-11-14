namespace OmniMonitor.Server.Configuration
{

    /// <summary>
    /// Represents a single set of credentials (Email and Password).
    /// </summary>
    public class CredentialDetails
    {
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
