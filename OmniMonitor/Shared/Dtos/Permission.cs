using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Representa un permiso en el sistema basado en módulos y acciones
    /// </summary>
    public class Permission
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nombre del módulo (ej: Users, Invoices, Dashboards)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Module { get; set; } = string.Empty;

        /// <summary>
        /// Acción permitida sobre el módulo (ej: View, Create, Edit, Delete)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Nombre completo del permiso en formato Module.Action (ej: Users.Create)
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        // Relación con roles
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
