using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniMonitor.Shared.Dtos
{
    public class DatasetAsset
    {
        [Key]
        public int Grupo_Asset { get; set; }

        [Required]
        public string Id_Asset { get; set; }

        // Clave foránea a DatasetAM
        public int DatasetAMId { get; set; }
        [ForeignKey("DatasetAMId")]
        public virtual DatasetAM DatasetAM { get; set; }
    }
}
