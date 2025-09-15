using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Tabla de relación muchos a muchos entre Usuario y Rol
    /// </summary>
    public class UserRole
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        [Required]
        public int RoleId { get; set; }
        public virtual Role Role { get; set; } = null!;

    }
}
