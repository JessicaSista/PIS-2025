namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Represents the data returned to the client after a login attempt.
    /// </summary>
    public class LoginResponse
    {
        /// <summary>
        /// Indicates whether the login was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// A message providing details about the login attempt (e.g., "Login successful" or an error message).
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// The JWT authentication token, provided on successful login.
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// The unique ID of the logged-in user.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        // The username of the logged-in user.
        /// </summary>
        public string? Username { get; set; }
    }
}