using System.ComponentModel.DataAnnotations;

public class CrossModuleJoin
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } // e.g., "Device Name to Event Name Link"

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(256)]
    public string Username { get; set; } = string.Empty;

    public JoinType JoinType { get; set; } // Inner Join, Left Join, etc.

    // --- The two sides of the join ---
    public int LeftOperandId { get; set; }
    public virtual JoinOperand LeftOperand { get; set; }

    public int RightOperandId { get; set; }
    public virtual JoinOperand RightOperand { get; set; }
}