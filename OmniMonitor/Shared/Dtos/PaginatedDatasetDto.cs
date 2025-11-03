namespace OmniMonitor.Shared.Dtos
{
    public class PaginatedDatasetDto
    {
        public List<DatasetDto> Items { get; set; } = new List<DatasetDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }
}
