using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class RoleDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; } // nullable

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty; // minLength: 1

        [JsonPropertyName("permissions")]
        public List<PermissionDto>? Permissions { get; set; }
    }
}
