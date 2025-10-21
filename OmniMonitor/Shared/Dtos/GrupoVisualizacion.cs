using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Representa la relación entre un dashboard y las visualizaciones que contiene
    /// </summary>
    public class GrupoVisualizacion
    {
        [Key]
        [Column("id_grupo_visualizacion")]
        public int IdGrupoVisualizacion { get; set; }

        
        [Required]
        [Column("grupo_visualizacion")]
        public int GrupoVisualizacionId { get; set; }

        [Required]
        [Column("id_visualizacion")]
        public int IdVisualizacion { get; set; }

        [Required]
        [Column("tipo_card")]
        public int TipoCard { get; set; } // 1=gráfica, 2=KPI, etc.

        [Column("props_configuracion")]
        [MaxLength(2000)]
        public string? PropsConfiguracion { get; set; }

        [Column("fecha_agregado")]
        public DateTime FechaAgregado { get; set; } = DateTime.UtcNow;

        [Column("orden")]
        public int Orden { get; set; } // Orden de la tarjeta en el dashboard

        // Propiedades de navegación
        public virtual DashboardDto? Dashboard { get; set; }
        public virtual Visualizacion? Visualizacion { get; set; }
        //public virtual KPI? KPI { get; set; }
    }
}
