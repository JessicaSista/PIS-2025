using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class UserRoleBundleDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("bundleDto")]
        public BundleDto? BundleDto { get; set; }

        [JsonPropertyName("userDto")]
        public UserDto? UserDto { get; set; }

        [JsonPropertyName("roleDto")]
        public RoleDto? RoleDto { get; set; }
    }
}
