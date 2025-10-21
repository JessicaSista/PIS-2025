using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class AssetTypeDto
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }


        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty; // minLength: 1


        //[JsonPropertyName("icon")]
        //public string? Icon { get; set; } // string($byte), nullable

        [JsonPropertyName("sourceId")]
        public int? SourceId { get; set; }

        [JsonPropertyName("source")]
        public SourceDto? Source { get; set; }


        //[JsonPropertyName("fieldDtos")]
        //public List<AssetTypeFieldDto>? FieldDtos { get; set; } // nullable

        [JsonPropertyName("bundleIds")]
        public List<int>? BundleIds { get; set; }

        [JsonPropertyName("bundleDtos")]
        public List<BundleDto>? BundleDtos { get; set; }


        [JsonPropertyName("subscriptions")]
        public List<AssetTypeSubscriptionDto>? Subscriptions { get; set; } // nullable
    }
}
