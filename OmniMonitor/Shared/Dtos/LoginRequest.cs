using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "El campo Usuario/Email es obligatorio.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "El campo Contraseña es obligatorio.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}