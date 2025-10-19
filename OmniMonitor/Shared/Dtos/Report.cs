using OmniMonitor.Shared.Dtos;
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

    public virtual ICollection<DatasetReports> DatasetsReports { get; set; } = new List<DatasetReports>();
    public virtual ICollection<ReportJoin> ReportJoins { get; set; } = new List<ReportJoin>();
    public string JSON_config { get; set; }
}