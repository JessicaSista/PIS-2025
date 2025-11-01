using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniMonitor.Shared.Dtos
{
    public class DatasetUM
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

        // Indica el tipo de contenido si Is_Dataset = 'N' (ej: "News", "Event", "Zone")
        public string? ContentType { get; set; }

        // Relación con los events seleccionados explícitamente
        public virtual ICollection<DatasetEvent> DatasetEvents { get; set; } = new List<DatasetEvent>();

        // Relación con los news seleccionados explícitamente
        public virtual ICollection<DatasetNews> DatasetNews { get; set; } = new List<DatasetNews>();

        // IDs para la búsqueda dinámica cuando no se seleccionan explícitamente
        public int? Id_Zone { get; set; }
        public int DatasetId { get; set; }  // Clave foránea
        [ForeignKey(nameof(DatasetId))]
        public virtual Datasets Datasets { get; set; } = null!;

    }
}
