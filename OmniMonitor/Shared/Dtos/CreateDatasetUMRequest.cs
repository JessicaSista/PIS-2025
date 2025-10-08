using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniMonitor.Shared.Dtos
{
    public class CreateDatasetUMRequest
    {
        public string Name { get; set; }
        public string Username { get; set; }
        public string? Description { get; set; }

        // 'S' si el usuario selecciono crear un nuevo dataset
        // 'N' si el usuario decido agregar solo UN event o news o zone 
        public string IsDataset { get; set; }

        // Si IsDataset = 'S'
        // ContentType = 0 -- para indicar nada
        // Si IsDataset = 'N'
        // Indicar que se selecciono
        // ContentType = 1 -- event
        // ContentType = 2 -- news
        // ContentType = 3 -- zone
        public string? ContentType { get; set; }
        public int? ZoneId { get; set; }
        public int? NewsId { get; set; }
        public string? EventName { get; set; }
        public List<int>? EventIds { get; set; }
        public List<int>? NewsIds { get; set; }
    }
}
