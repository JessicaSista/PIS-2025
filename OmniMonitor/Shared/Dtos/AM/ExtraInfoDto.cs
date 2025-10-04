using System;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos.AM
{
    public class ExtraInfoDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("text")]
        public string? Text { get; set; }
        [JsonPropertyName("parentId")]
        public int ParentId { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("hasFileData")]
        public bool HasFileData { get; set; }
        [JsonPropertyName("isInInstance")]
        public bool IsInInstance { get; set; }
        [JsonPropertyName("fileData")]
        public string? FileData { get; set; }
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }
        [JsonPropertyName("addedBy")]
        public UserDto? AddedBy { get; set; }
    }
}