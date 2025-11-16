using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos.EM
{
    public class AlertDto
    {
        public int AlertId { get; set; }
        public string AlertName { get; set; } = string.Empty;
        public int SourceId { get; set; }
        public EMLocationDto? Location { get; set; }
        public List<EMLocationDto> LocationHistory { get; set; } = new();
        public string SourceAddress { get; set; } = string.Empty;
        public string AlertState { get; set; } = string.Empty;
        public AlertCategoryDto? AlertCategory { get; set; }
        public string AlertData { get; set; } = string.Empty;
        [JsonPropertyName("creationDate")]
        public DateTime CreatedAt { get; set; }
        [JsonPropertyName("lastModification")]
        public DateTime ModifiedAt { get; set; }
        public int DeviceType { get; set; }
        public string StreamUrl { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
    }


}
