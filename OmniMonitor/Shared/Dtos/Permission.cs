using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Representa un permiso en el sistema
    /// </summary>
    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        // Relación con roles
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
