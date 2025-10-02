using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniMonitor.Shared.Dtos
{
    public class GrupoDataset
    {
        [Key]
        [Column("id")]
        public int IdGroupDataset { get; set; }

        [Column("Id_visualizacion")]
        public int VisualizacionId { get; set; }

        [ForeignKey("VisualizacionId")]
        public virtual Visualizacion Visualizacion { get; set; }

        [Column("id_dataset")]
        public int DatasetId { get; set; }

        [ForeignKey("DatasetId")]
        public virtual Dataset Dataset { get; set; }

        [Column("JSON_design")]
        [MaxLength(1000)]
        public string JsonDesign { get; set; }
    }
}
