using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniMonitor.Shared.Dtos
{
    public class DatasetEventTaskInstance
    {
        [Key]
        public int Id { get; set; }

    public int DatasetAMId { get; set; }
    [ForeignKey("DatasetAMId")]
    public virtual DatasetAM DatasetAM { get; set; }

        [Required]
        public int Id_Event_Task_Instance { get; set; }

        public ICollection<DatasetStock>? Grupo_Stock { get; set; } = new List<DatasetStock>();
    }
}
