using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniMonitor.Shared.Dtos
{
    public class Dataset
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [MaxLength(1)]
        public string EsDataset { get; set; } = "S"; // 'S' para dataset creado por usuario, 'N' para dataset interno

        [Required]
        public int IdUsuario { get; set; }

        [ForeignKey("IdUsuario")]
        public User? Usuario { get; set; }

        [MaxLength(50)]
        public string? GrupoDevice { get; set; }

        public int? IdSource { get; set; }

        public int? IdGroup { get; set; }

        [Required]
        public int IdSensor { get; set; }

        [MaxLength(10)]
        public string? TipoEntidad { get; set; } // 'device', 'source', 'group', 'sensor'

        [MaxLength(10)]
        public string? Modulo { get; set; } = "IM"; // 'IM', 'AM', 'UM', etc.

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

        // Navegación
        public virtual ICollection<DeviceGrupo> DeviceGrupos { get; set; } = new List<DeviceGrupo>();
    }
}