using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Many-to-many relationship table between Role and Permission
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
