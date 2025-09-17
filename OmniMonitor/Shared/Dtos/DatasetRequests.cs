using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Request para crear un nuevo dataset
    /// </summary>
    public class CreateDatasetRequest
    {
        [Required(ErrorMessage = "El nombre del dataset es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "El módulo es obligatorio")]
        public string Module { get; set; } = string.Empty;

        // Referencias a datos externos (solo IDs)
        public int? SourceId { get; set; } // ID de la fuente externa
        public int? DeviceGroupId { get; set; } // ID del grupo de dispositivos externo

        // Listas de IDs de sensores y dispositivos externos
        public List<string> SensorIds { get; set; } = new List<string>();
        public List<int> DeviceIds { get; set; } = new List<int>();

        public int UserId { get; set; } // Usuario que crea el dataset
        public int? TenantId { get; set; } // Para multi-tenancy
    }

    /// <summary>
    /// Request para actualizar un dataset existente
    /// </summary>
    public class UpdateDatasetRequest
    {
        [Required(ErrorMessage = "El nombre del dataset es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "El módulo es obligatorio")]
        public string Module { get; set; } = string.Empty;

        public int? SourceId { get; set; }
        public int? DeviceGroupId { get; set; }

        public List<string> SensorIds { get; set; } = new List<string>();
        public List<int> DeviceIds { get; set; } = new List<int>();

        public int UserId { get; set; } // Usuario que actualiza el dataset
        public int? TenantId { get; set; }
    }
}
