using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos.AM
{
    public class TaskActionDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("active")]
        public bool Active { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("completed")]
        public bool Completed { get; set; }
        [JsonPropertyName("userName")]
        public string? UserName { get; set; }
        [JsonPropertyName("comment")]
        public string? Comment { get; set; }
        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }
        [JsonPropertyName("value")]
        public int Value { get; set; }
        [JsonPropertyName("bundleIds")]
        public List<int>? BundleIds { get; set; }
        [JsonPropertyName("bundleDtos")]
        public List<BundleDto>? BundleDtos { get; set; }
    }
}