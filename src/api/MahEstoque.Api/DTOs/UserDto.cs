using System.ComponentModel.DataAnnotations;

namespace MahEstoque.Api.DTOs;

public class CreateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = "Employee";
}

public class UpdateUserRequest
{
    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    public string? Role { get; set; }

    [MinLength(6)]
    public string? Password { get; set; }
}

public class UserListItemDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}