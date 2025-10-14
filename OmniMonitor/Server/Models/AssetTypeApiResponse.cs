using System.Collections.Generic;
using OmniMonitor.Shared.Dtos.AM;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Models
{
    public class AssetTypeApiResponse
    {
        public List<AssetTypeDto> Results { get; set; } = new();
    }
}
