namespace OmniMonitor.Shared.Dtos.EM
{
    public class PlaneDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageData { get; set; }
        public int SectorId { get; set; }
        public int PixelsWidth { get; set; }
        public int PixelsHeight { get; set; }
        public EMLocationDto? Location { get; set; }
        public int WorkZoneId { get; set; }
        public string? WorkZoneName { get; set; }
        public int UploadState { get; set; }
        public string FormImage { get; set; } = string.Empty;
    }
}
