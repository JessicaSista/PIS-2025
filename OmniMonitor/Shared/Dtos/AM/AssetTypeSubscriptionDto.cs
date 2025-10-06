using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class AssetTypeSubscriptionDto
    {
        [JsonPropertyName("assetType")]
        public AssetTypeDto? AssetType { get; set; }

        [JsonPropertyName("user")]
        public UserDto? User { get; set; }

        [JsonPropertyName("alertTypes")]
        public List<string>? AlertTypes { get; set; } // nullable
    }
}
