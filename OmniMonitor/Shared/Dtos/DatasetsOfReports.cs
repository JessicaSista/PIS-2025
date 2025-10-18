
using System.ComponentModel.DataAnnotations;

public class DatasetsOfReports
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public ModuleType ModuleType { get; set; }

    [Required]
    [MaxLength(256)]
    public int id_dataset { get; set; }
}

