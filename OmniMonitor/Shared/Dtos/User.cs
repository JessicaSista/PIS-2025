using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Represents a user of YOUR application.
    /// This entity is stored in your database.
    /// </summary>
    [Index(nameof(Username), IsUnique = true)]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
        public string? SondaToken { get; set; }

        public DateTime? TokenExpiration { get; set; }

        // Relación con roles
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}