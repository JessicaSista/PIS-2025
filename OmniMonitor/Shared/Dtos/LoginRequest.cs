using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
