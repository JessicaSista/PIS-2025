namespace OmniMonitor.Shared.Dtos.EM
{
    public class ExtensionApiResponse
    {
        public List<ExtensionDto> Results { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
        public int CurrentPage { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }
}
