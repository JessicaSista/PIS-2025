using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Represents a user of YOUR application.
/// This entity is stored in your database.
/// </summary>
[Index(nameof(Username), IsUnique = true)]
public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Username { get; set; }

    [Required]
    public string Password { get; set; }
    public string? SondaToken { get; set; }

    public DateTime? TokenExpiration { get; set; }
}