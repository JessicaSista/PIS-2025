namespace OmniMonitor.Server.Configuration
{

    /// <summary>
    /// Represents the nested "Credentials" object, which contains multiple credential sets.
    /// </summary>
    public class CredentialsConfig
    {
        public CredentialDetails? CredentialsIM { get; set; }

        public CredentialDetails? CredentialsAM { get; set; }

        public CredentialDetails? CredentialsEM { get; set; }

        public CredentialDetails? CredentialsUM { get; set; }
    }
}