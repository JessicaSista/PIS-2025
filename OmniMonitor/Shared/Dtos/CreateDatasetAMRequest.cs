using System.Collections.Generic;

namespace OmniMonitor.Shared.Dtos
{
    public class CreateDatasetAMRequest
    {
        public string Username { get; set; }
        public string Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string IsDataset { get; set; }
        public string? ContentType { get; set; }
        public int Type_Dataset { get; set; } // 1 = EventTask, 2 = Asset
        public int? Id_Event_Task { get; set; }
        public int? Id_Asset_Type { get; set; }
        public List<int>? Grupo_Event_Task_Instance_Ids { get; set; }
        public List<string>? Grupo_Asset_Ids { get; set; }
        public List<int>? StockIds { get; set; }
    }
}
