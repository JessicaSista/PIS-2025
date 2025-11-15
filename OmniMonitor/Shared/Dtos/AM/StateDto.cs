using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class StateDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("bundleIds")]
        public List<int>? BundleIds { get; set; }

        [JsonPropertyName("bundleDtos")]
        public List<BundleDto>? BundleDtos { get; set; }

        public override string ToString() => Name;
    }
}
