using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos.AM
{
    public class AprobationConfigurationDto
    {
        [JsonPropertyName("userId")]
        public string? UserId { get; set; }
        [JsonPropertyName("user")]
        public UserDto? User { get; set; }
        [JsonPropertyName("groupId")]
        public int? GroupId { get; set; }
        [JsonPropertyName("group")]
        public GroupDto? Group { get; set; }
    }
}