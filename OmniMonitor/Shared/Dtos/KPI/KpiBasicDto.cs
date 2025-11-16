using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class KpiSimpleDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("defaultColor")]
        public string? DefaultColor { get; set; }
    }

    public class KpiSimplePaginatedResponse
    {
        [JsonPropertyName("items")]
        public List<KpiSimpleDto> Items { get; set; } = new List<KpiSimpleDto>();

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("hasPreviousPage")]
        public bool HasPreviousPage { get; set; }

        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; set; }
    }
}