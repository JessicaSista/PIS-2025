using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Representa un rol en el sistema
    /// </summary>
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        // Relación con permisos
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        // Relación con usuarios
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
