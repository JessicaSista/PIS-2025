using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniMonitor.Shared.Dtos
{
    public class DatasetStock
    {
        [Key]
        public int Grupo_Stock { get; set; }

        [Required]
        public int Id_Stock { get; set; }

        // Clave foránea a DatasetAM
        public int DatasetAMId { get; set; }
        [ForeignKey("DatasetAMId")]
        [System.Text.Json.Serialization.JsonIgnore]
        public DatasetAM? DatasetAM { get; set; }
    }
}
