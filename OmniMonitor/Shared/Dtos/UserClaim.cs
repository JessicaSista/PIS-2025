using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Representa un claim específico asignado directamente a un usuario
    /// (adicional a los permisos heredados de roles)
    /// </summary>
    public class UserClaim
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        [Required]
        public int PermissionId { get; set; }
        public virtual Permission Permission { get; set; } = null!;

        /// <summary>
        /// Indica si este claim fue agregado (true) o removido (false) explícitamente del usuario
        /// Permite sobrescribir permisos heredados de roles
        /// </summary>
        public bool IsGranted { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

