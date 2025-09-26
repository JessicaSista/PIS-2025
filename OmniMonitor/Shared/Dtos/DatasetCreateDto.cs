using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Shared.Dtos
{
    public class DatasetCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string TipoEntidad { get; set; } = string.Empty; // 'device', 'source', 'group', 'sensor', etc.

        [MaxLength(10)]
        public string Modulo { get; set; } = "IM"; // 'IM', 'AM', 'UM', etc.

        [MaxLength(50)]
        public string? GrupoDevice { get; set; }

        public int? IdSource { get; set; }

        public int? IdGroup { get; set; }

        [Required]
        public int IdSensor { get; set; }

        public List<int>? IdDevices { get; set; } // Lista de IDs de dispositivos seleccionados

        [MaxLength(1)]
        public string EsDataset { get; set; } = "S"; // 'S' para dataset creado por usuario, 'N' para dataset interno
    }
}
