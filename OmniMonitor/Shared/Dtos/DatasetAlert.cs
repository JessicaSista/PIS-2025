using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniMonitor.Shared.Dtos
{
    public class DatasetAlert
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Id_alert { get; set; }

        // Relación de clave externa con Dataset
        public int DatasetId { get; set; }
        [ForeignKey("DatasetId")]
        public virtual DatasetEM Dataset { get; set; }
    }
}
