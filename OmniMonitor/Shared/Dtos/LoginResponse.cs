namespace OmniMonitor.Shared.Dtos
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public User? User { get; set; }
        public List<string>? Roles { get; set; }
    }
}
