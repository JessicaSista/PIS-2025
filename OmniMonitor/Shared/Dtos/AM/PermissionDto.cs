using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class PermissionDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
