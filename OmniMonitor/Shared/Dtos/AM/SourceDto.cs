using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
	public class SourceDto
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("name")]
		public string? Name { get; set; } // nullable

		[JsonPropertyName("assetType")]
		public AssetTypeDto? AssetType { get; set; }

		[JsonPropertyName("devices")]
		public List<DeviceDto>? Devices { get; set; } // nullable
	}
}