using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class AssetDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("externalId")]
        public string? ExternalId { get; set; } // nullable

        [JsonPropertyName("code")]
        public string? Code { get; set; } // nullable

        [JsonPropertyName("locationDto")]
        public LocationDto? LocationDto { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; } // nullable

        [JsonPropertyName("reference")]
        public string? Reference { get; set; } // nullable

        [JsonPropertyName("barCode")]
        public string? BarCode { get; set; } // nullable

        [JsonPropertyName("qrCode")]
        public string? QrCode { get; set; } // nullable

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; } // string($date-time)

        [JsonPropertyName("lifeTimeToDate")]
        public int? LifeTimeToDate { get; set; } // integer($int32)

        [JsonPropertyName("typeDto")]
        public AssetTypeDto TypeDto { get; set; } = new AssetTypeDto(); // required

        [JsonPropertyName("bundleId")]
        public int BundleId { get; set; } // required

        [JsonPropertyName("bundleDto")]
        public BundleDto? BundleDto { get; set; }

        [JsonPropertyName("brandDto")]
        public BrandDto BrandDto { get; set; } = new BrandDto(); // required

        [JsonPropertyName("stateDto")]
        public StateDto StateDto { get; set; } = new StateDto(); // required

        [JsonPropertyName("modelDto")]
        public ModelDto ModelDto { get; set; } = new ModelDto(); // required

        [JsonPropertyName("responsibleDto")]
        public ResponsibleDto? ResponsibleDto { get; set; } // required

        [JsonPropertyName("providerDto")]
        public ProviderDto? ProviderDto { get; set; } // required

        [JsonPropertyName("device")]
        public DeviceDto? Device { get; set; }

        [JsonPropertyName("predictionFailure")]
        public AssetPredictionFailureDto? PredictionFailure { get; set; }

        [JsonPropertyName("deliveredTo")]
        public UserDto? DeliveredTo { get; set; }

    }
}
