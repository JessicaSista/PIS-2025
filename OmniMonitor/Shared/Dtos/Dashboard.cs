using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    public class DashboardDto
    {
        [Key]
        public int Id { get; set; }   // Identificador único del dashboard

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;  // Nombre del dashboard

        [MaxLength(200)]
        public string? Description { get; set; }          // Descripción opcional

        [Required]
        [MaxLength(256)]
        public string Username { get; set; } = string.Empty;  // Usuario propietario

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Fecha de creación

        public DateTime? UpdatedAt { get; set; } = null;          // Última modificación opcional

        // Relación opcional con widgets u otros elementos del dashboard
        //public virtual ICollection<DashboardWidget>? DashboardWidgets { get; set; } = new List<DashboardWidget>();
    }
}
