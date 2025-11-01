using Microsoft.AspNetCore.Identity;

using OmniMonitor.Shared.Dtos;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniMonitor.Server.Models
{
    public enum ShareVisibility { Public, Private }
    public enum ShareStatus { Active, Hidden }


    public class SharedLink
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Slug { get; set; }

        [Required]
        public int DashboardId { get; set; }

        [Required]
        public ShareVisibility Visibility { get; set; } = ShareVisibility.Public;

        [Required]
        public ShareStatus Status { get; set; } = ShareStatus.Active;

        public string? PasswordHash { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiresAt { get; set; }

        [ForeignKey("DashboardId")]
        public virtual DashboardDto Dashboard { get; set; }
    }
}
