using System.ComponentModel.DataAnnotations;

public class JoinOperand
{
    [Key]
    public int Id { get; set; }

    // Identifies which module dataset to use
    public ModuleType ModuleType { get; set; } // "IM", "UM", "EM", "AM"
    public int DatasetId { get; set; } // The ID of the DatasetIM, DatasetUM, etc.

    // The name of the entity we are fetching from the API
    // e.g., "Device", "Event", "Alert"
    [Required]
    [MaxLength(100)]
    public EntityName EntityName { get; set; }

    // The property of the entity to use as the join key
    // e.g., "Name", "SourceId", "TypeId"
    [Required]
    [MaxLength(100)]
    public string JoinPropertyName { get; set; } = string.Empty;
}