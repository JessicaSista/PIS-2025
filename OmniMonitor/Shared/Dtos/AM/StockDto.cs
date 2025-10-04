using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos.AM
{
    public class StockDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
        [JsonPropertyName("provider")]
        public ProviderDto Provider { get; set; } = new ProviderDto();
        [JsonPropertyName("location")]
        public string? Location { get; set; }
        [JsonPropertyName("sku")]
        public string? Sku { get; set; }
        [JsonPropertyName("minimum")]
        public int Minimum { get; set; }
        [JsonPropertyName("extraInfoDtos")]
        public List<ExtraInfoDto>? ExtraInfoDtos { get; set; }
        [JsonPropertyName("bundleId")]
        public int BundleId { get; set; }
        [JsonPropertyName("bundle")]
        public BundleDto? Bundle { get; set; }
        [JsonPropertyName("supervisor")]
        public UserDto? Supervisor { get; set; }
        [JsonPropertyName("categories")]
        public List<string>? Categories { get; set; }
        [JsonPropertyName("updatedQuantity")]
        public int UpdatedQuantity { get; set; }
        [JsonPropertyName("extraInfoRemoved")]
        public List<int>? ExtraInfoRemoved { get; set; }
    }
}