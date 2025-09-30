using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniMonitor.Shared.Dtos
{
    public class Visualizacion
    {
        [Key]
        [Column("Id")]
        public int IdVisualizacion { get; set; }

        [Required]
        [MaxLength(256)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Column("Date_from")]
        public DateTime FechaDesde { get; set; }

        [Column("Date_to")]
        public DateTime FechaHasta { get; set; }

        [Column("JSON_design")]
        [MaxLength(1000)]
        public string JsonDesign { get; set; }

        // Propiedad de navegación para la relación uno a muchos
        public virtual ICollection<GrupoDataset> GrupoDatasets { get; set; } = new List<GrupoDataset>();
    }
}