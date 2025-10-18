using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniMonitor.Shared.Dtos
{
    public class CreateDatasetEMRequest
    {
        public string Name { get; set; }
        public string Username { get; set; }
        public string? Description { get; set; }

        // 'S' si el usuario selecciono crear un nuevo dataset
        // 'N' si el usuario decido agregar solo UN alert, event, extension o resource 
        public string IsDataset { get; set; }

        // Si IsDataset = 'S'
        // ContentType = 0 -- para indicar nada
        // Si IsDataset = 'N'
        // Indicar que se selecciono
        // ContentType = 1 -- alert
        // ContentType = 2 -- event
        // ContentType = 3 -- extension
        // ContentType = 4 -- resource
        public string? ContentType { get; set; }

        // Filtros para búsqueda dinámica
        public int? AlertId { get; set; }
        public int? EventId { get; set; }
        public int? ExtensionId { get; set; }
        public int? CategoryId { get; set; }
        public string? AlertState { get; set; }
        public string? EventState { get; set; }
        public string? ExtensionState { get; set; }
        public string? CategoryState { get; set; }

        // IDs de entidades específicas
        public List<int>? AlertIds { get; set; }
        public List<int>? EventIds { get; set; }
        public List<int>? ExtensionIds { get; set; }
        public List<int>? CategoryIds { get; set; }
    }
}
