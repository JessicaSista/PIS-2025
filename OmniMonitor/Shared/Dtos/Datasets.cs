using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniMonitor.Shared.Dtos
{ 
    public class Datasets
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string NameDataset { get; set; }= string.Empty;

        public ModuleType TipoDataset { get; set; }

        // Relación con los DatasetsEM
        public virtual ICollection<DatasetEM> DatasetEM { get; set; } = new List<DatasetEM>();

        // Relación con los DatasetsUM
        public virtual ICollection<DatasetUM> DatasetUM { get; set; } = new List<DatasetUM>();
        // Relación con los DatasetAM
        public virtual ICollection<DatasetAM> DatasetAM { get; set; } = new List<DatasetAM>();

        // Relación con los DatasetIM
        public virtual ICollection<DatasetIM> DatasetIM { get; set; } = new List<DatasetIM>();
    }
}
