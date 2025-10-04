namespace OmniMonitor.Shared.Dtos.EM
{
    public class SchemaFieldDto
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string? DefaultValue { get; set; }
        public string? Validation { get; set; }
        public string? PossibleValues { get; set; }
        public bool Automatic { get; set; }
    }
}
