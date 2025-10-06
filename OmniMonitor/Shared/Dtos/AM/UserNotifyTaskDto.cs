using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos.AM
{
    public class UserNotifyTaskDto
    {
        [JsonPropertyName("taskTypeId")]
        public int TaskTypeId { get; set; }
        [JsonPropertyName("taskTypeDto")]
        public TaskTypeDto? TaskTypeDto { get; set; }
        [JsonPropertyName("aprobationConfiguration")]
        public AprobationConfigurationDto? AprobationConfiguration { get; set; }
        [JsonPropertyName("bundleIds")]
        public List<int>? BundleIds { get; set; }
        [JsonPropertyName("bundleDtos")]
        public List<BundleDto>? BundleDtos { get; set; }
        [JsonPropertyName("actions")]
        public List<TaskActionDto>? Actions { get; set; }
        [JsonPropertyName("assetChangeStates")]
        public List<TaskTypeChangeAssetStateDto>? AssetChangeStates { get; set; }
        [JsonPropertyName("stockQuantities")]
        public Dictionary<string, int>? StockQuantities { get; set; }
        [JsonPropertyName("stockDtos")]
        public List<StockDto>? StockDtos { get; set; }
        [JsonPropertyName("userIds")]
        public string? UserIds { get; set; }
        [JsonPropertyName("users")]
        public List<UserDto>? Users { get; set; }
    }
}