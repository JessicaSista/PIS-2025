using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class GroupDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("supervisor")]
        public UserDto? Supervisor { get; set; }

        [JsonPropertyName("userDtos")]
        public List<UserDto>? UserDtos { get; set; }

        [JsonPropertyName("userIds")]
        public List<string>? UserIds { get; set; }

        [JsonPropertyName("bundleDtos")]
        public List<BundleDto>? BundleDtos { get; set; }

        [JsonPropertyName("bundleIds")]
        public List<int>? BundleIds { get; set; }
    }
}
