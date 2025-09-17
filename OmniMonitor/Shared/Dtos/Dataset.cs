using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    /// <summary>
    /// Entidad Dataset que representa un conjunto de datos configurado por el usuario
    /// </summary>
    public class Dataset
    {
        public int Id { get; set; }

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

        // Metadatos
        public int UserId { get; set; } // Usuario que creó el dataset
        public int? TenantId { get; set; } // Para multi-tenancy
    }
}
