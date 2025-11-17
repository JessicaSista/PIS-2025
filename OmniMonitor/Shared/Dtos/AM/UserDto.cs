using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class UserDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("userName")]
        public string? UserName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("newPassword")]
        public string? NewPassword { get; set; }

        [JsonPropertyName("oldPassword")]
        public string? OldPassword { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }

        [JsonPropertyName("roleDtos")]
        public List<RoleDto>? RoleDtos { get; set; }

        [JsonPropertyName("bundleDtos")]
        public List<BundleDto>? BundleDtos { get; set; }

        [JsonPropertyName("groupDtos")]
        public List<GroupDto>? GroupDtos { get; set; }

        [JsonPropertyName("userRoleIds")]
        public List<string>? UserRoleIds { get; set; }

        [JsonPropertyName("userBundleIds")]
        public List<string>? UserBundleIds { get; set; }

        [JsonPropertyName("userRoleBundleDtos")]
        public List<UserRoleBundleDto>? UserRoleBundleDtos { get; set; }

        public override string ToString() => UserName ?? string.Empty;
    }
}
