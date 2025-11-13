using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniMonitor.Shared.Dtos
{
    public class DatasetEM
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(1)] // 'S' o 'N'
        public string Is_Dataset { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        // Indica el tipo de contenido si Is_Dataset = 'N' (ej: "Alert", "Event", "Extension", "Resource")
        public string? ContentType { get; set; }

        // Relación con los alerts seleccionados explícitamente
        public virtual ICollection<DatasetAlert> DatasetAlerts { get; set; } = new List<DatasetAlert>();

        // Relación con los events seleccionados explícitamente
        public virtual ICollection<DatasetEventEM> DatasetEvents { get; set; } = new List<DatasetEventEM>();

        // Relación con las extensions seleccionadas explícitamente
        public virtual ICollection<DatasetExtension> DatasetExtensions { get; set; } = new List<DatasetExtension>();

        // Relación con los resources seleccionados explícitamente
        public virtual ICollection<DatasetCategory> DatasetCategory { get; set; } = new List<DatasetCategory>();
        public int DatasetId { get; set; }  // Clave foránea
        
        /// <summary>
        /// Filtros aplicados almacenados como JSON. 
        /// Contiene un array de FilterCondition serializados.
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? Filters { get; set; }
        
        [ForeignKey(nameof(DatasetId))]
        public virtual Datasets Datasets { get; set; } = null!;


    }
}
