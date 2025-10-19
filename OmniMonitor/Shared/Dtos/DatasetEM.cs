using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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

        // IDs para la búsqueda dinámica cuando no se seleccionan explícitamente
        public int? Id_Alert { get; set; }
        public int? Id_Event { get; set; }
        public int? Id_Extension { get; set; }
        public int? Id_Category { get; set; }
        public string? AlertState { get; set; }
        public string? EventState { get; set; }
        public string? ExtensionState { get; set; }
        public string? CategoryState { get; set; }
    }
}
