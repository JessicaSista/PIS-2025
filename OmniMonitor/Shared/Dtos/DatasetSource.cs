using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniMonitor.Shared.Dtos
{
    public class DatasetSource
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DatasetId { get; set; }

        [Required]
        public int Id_source { get; set; }

        // Relación con el dataset
        [ForeignKey(nameof(DatasetId))]
        public virtual DatasetIM Dataset { get; set; } = null!;
    }
}