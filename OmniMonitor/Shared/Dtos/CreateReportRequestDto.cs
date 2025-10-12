using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO for creating a new report. Contains report metadata and a list of included joins.
/// </summary>
public class CreateReportRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(256)]
    public string Username { get; set; } = string.Empty;

    // This property holds the collection of joins for the new report.
    public ICollection<ReportJoinItemDto> ReportJoins { get; set; } = new List<ReportJoinItemDto>();
}