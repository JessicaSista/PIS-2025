using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Tabla de relación muchos a muchos entre Rol y Permiso
    /// </summary>
    public class RolePermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RoleId { get; set; }
        public virtual Role Role { get; set; } = null!;

        [Required]
        public int PermissionId { get; set; }
        public virtual Permission Permission { get; set; } = null!;

    }
}
