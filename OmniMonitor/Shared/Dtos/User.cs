using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace OmniMonitor.Shared.Dtos
{
    public class User : IdentityUser<int>
    {
        public string? SondaTokenIM { get; set; }

        public DateTime? TokenExpirationIM { get; set; }

        public string? SondaTokenAM { get; set; }

        public DateTime? TokenExpirationAM { get; set; }

        public string? SondaTokenUM { get; set; }

        public DateTime? TokenExpirationUM { get; set; }

        public string? SondaTokenEM { get; set; }

        public DateTime? TokenExpirationEM { get; set; }

        public string? SondaTokenOM { get; set; }

        public DateTime? TokenExpirationOM { get; set; }

        // Relación con roles
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        // Relación con claims/permisos específicos del usuario
        public virtual ICollection<UserClaim> UserClaims { get; set; } = new List<UserClaim>();
    }
}