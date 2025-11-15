using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniMonitor.Shared.Dtos
{
    public class CreateDatasetIMRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }

        // 'S' si el usuario selecciono crear un nuevo dataset
        // 'N' si el usuario decido agregar solo UN device o source o sensor 
        public string IsDataset { get; set; }

        // Si IsDataset = 'S'
        // ContentType = 0 -- para indicar nada
        // Si IsDataset = 'N'
        // Indicar que se selecciono
        // ContentType = 1 -- device
        // ContentType = 2 -- source
        // ContentType = 3 -- sensor
        public string? ContentType { get; set; }
        public int? SourceId { get; set; }
        public int? GroupId { get; set; }
        public string? SensorName { get; set; }
        public List<int>? DeviceIds { get; set; }
        
        // Lista de filtros para datasets no formales
        public List<FilterCondition>? Filters { get; set; }
        
        // Campo interno para JsonFilters (se setea en el controller)
        public string? JsonFilters { get; set; }
    }
}
