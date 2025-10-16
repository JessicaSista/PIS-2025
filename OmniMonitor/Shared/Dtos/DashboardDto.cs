using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Representa un dashboard personalizable del usuario
    /// </summary>
    public class DashboardDto
    {
        [Key]
        [Column("id_dashboard")]
        public int IdDashboard { get; set; }

        [Required]
        [MaxLength(256)]
        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500)]
        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("grupo_visualizacion")]
        public int? GrupoVisualizacion { get; set; }

        [Column("JSON_diseño")]
        [MaxLength(4000)]
        public string? JsonDiseno { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Column("fecha_modificacion")]
        public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

        // Propiedad de navegación para la relación con GrupoVisualizacion
        public virtual ICollection<GrupoVisualizacion> GrupoVisualizaciones { get; set; } = new List<GrupoVisualizacion>();
    }
}
