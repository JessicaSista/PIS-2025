using System.ComponentModel.DataAnnotations;

public class Report
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(256)]
    public string Username { get; set; } = string.Empty;

    // This navigation property defines the many-to-many relationship
    // It will hold the collection of join configurations associated with this report.
    public virtual ICollection<ReportJoin> ReportJoins { get; set; } = new List<ReportJoin>();
}