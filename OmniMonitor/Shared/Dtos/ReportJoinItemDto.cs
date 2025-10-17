using System.ComponentModel.DataAnnotations;

public class ReportJoinItemDto
{
    [Required]
    public int CrossModuleJoinId { get; set; }

    public int ExecutionOrder { get; set; }
}