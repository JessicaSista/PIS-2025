using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO for creating a new report. Contains report metadata and a list of included joins.
/// </summary>////
public class CreateReportRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public string JSON_config { get; set; } = string.Empty;
    
    public string? JSON_filters { get; set; }
}
