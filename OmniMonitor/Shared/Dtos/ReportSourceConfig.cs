public class ReportSourceConfig
{
    public string SourceType { get; set; } = string.Empty;
    public int? SourceId { get; set; }
    public ModuleType? SourceModule { get; set; }

    public EntityName? EntityName { get; set; }
    public List<ReportColumnConfig> Columns { get; set; } = new();
}