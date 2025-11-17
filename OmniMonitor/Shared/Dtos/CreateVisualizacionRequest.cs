using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniMonitor.Shared.Dtos
{
    public class CreateVisualizacionRequest
    {
        [Required]
        public string Nombre { get; set; }

        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }

        public string JsonDiseñoGeneral { get; set; }

        public string? Link { get; set; }

        public List<DatasetConfig> Datasets { get; set; } = new List<DatasetConfig>();
    }
}
