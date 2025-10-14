public class JoinOperandDto
{
    public ModuleType ModuleType { get; set; }
    public int DatasetId { get; set; }
    public EntityName EntityName { get; set; }
    public string JoinPropertyName { get; set; } = string.Empty;
}