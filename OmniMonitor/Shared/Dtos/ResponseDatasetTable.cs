using System.Text.Json;

namespace OmniMonitor.Shared.Dtos
{
    public class ResponseDatasetTable
    {
        public int IdDatasetTable { get; set; }
        public string TipoDataset { get; set; }
        public JsonElement Data { get; set; }
    }
}
