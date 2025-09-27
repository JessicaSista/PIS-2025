using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    public class Dataset
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

        // Indica el tipo de contenido si Is_Dataset = 'N' (ej: "Device", "Source", "Group")
        public string? ContentType { get; set; }

        // Relación con los devices seleccionados explícitamente
        public virtual ICollection<DatasetDevice> DatasetDevices { get; set; } = new List<DatasetDevice>();

        // IDs para la búsqueda dinámica de devices cuando no se seleccionan explícitamente
        public int? Id_Source { get; set; }
        public int? Id_Group { get; set; }
        public string? SensorName { get; set; }
    }
}
