using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniMonitor.Shared.Dtos
{
    public class DeviceGrupo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string GrupoDevice { get; set; } = string.Empty;

        [Required]
        public int IdDevice { get; set; }

        [Required]
        public int IdDataset { get; set; }

        [ForeignKey("IdDataset")]
        public Dataset? Dataset { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
