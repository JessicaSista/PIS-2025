using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniMonitor.Shared.Dtos
{
    public class ShareRequestDto
    {
        [Required]
        public string Visibility { get; set; } = "public"; // "public" o "private"

        public DateTime? ExpiresAt { get; set; }

        // La contraseña (opcional) solo se usa si la visibilidad es "private"
        public string? Password { get; set; }
    }
}
