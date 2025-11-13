using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
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

        public bool IsGranted { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

