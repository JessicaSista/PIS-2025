using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    public class DatasetUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Descripcion { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? GrupoDevice { get; set; }

        public int? IdSource { get; set; }

        public int? IdGroup { get; set; }

        [Required]
        public int IdSensor { get; set; }

        public List<int>? IdDevices { get; set; } // Lista de IDs de dispositivos seleccionados
    }
}
