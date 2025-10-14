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

        // Clave foránea a DatasetEventTaskInstance
        public int DatasetEventTaskInstanceId { get; set; }
        [ForeignKey("DatasetEventTaskInstanceId")]
        public virtual DatasetEventTaskInstance DatasetEventTaskInstance { get; set; }
    }
}
