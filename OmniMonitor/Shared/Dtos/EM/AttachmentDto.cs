namespace OmniMonitor.Shared.Dtos.EM
{
    public class AttachmentDto
    {
        public int State { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string AddedBy { get; set; } = string.Empty;
        public int AttachmentId { get; set; }
        public bool IsAttachedItemFromEvent { get; set; }
        public int ExtensionId { get; set; }
    }
}
