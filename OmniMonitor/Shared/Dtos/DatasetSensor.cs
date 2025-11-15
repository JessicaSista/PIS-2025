using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniMonitor.Shared.Dtos
{
    public class DatasetSensor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DatasetId { get; set; }

        [Required]
        [MaxLength(255)]
        public string SensorName { get; set; } = string.Empty;

        // Relación con el dataset
        [ForeignKey(nameof(DatasetId))]
        public virtual DatasetIM Dataset { get; set; } = null!;
    }
}