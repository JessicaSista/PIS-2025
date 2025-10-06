namespace OmniMonitor.Shared.Dtos.EM
{
    public class TemplateDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Html { get; set; }
        public bool GenerateOnNewEvent { get; set; }
    }
}
