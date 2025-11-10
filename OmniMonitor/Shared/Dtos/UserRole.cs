using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Many-to-many relationship table between User and Role
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
