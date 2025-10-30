using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class VisualizationRequest
    {

        [JsonPropertyName("datasetId")]
        public int datasetId { get; set; }

        [JsonPropertyName("moduleType")]
        public ModuleType moduleType{ get; set; }


        [JsonPropertyName("entity")]
        public EntityName entity { get; set; }

        [JsonPropertyName("column")]
        public string column { get; set; }


        [JsonPropertyName("dateFrom")]
        public DateTime dateFrom{ get; set; }


        [JsonPropertyName("dateTo")]
        public DateTime dateTo { get; set; }

    }


}
