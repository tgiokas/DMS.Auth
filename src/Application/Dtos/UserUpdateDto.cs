using System.ComponentModel.DataAnnotations;

namespace DMS.Auth.Application.Dtos;

public class UserUpdateDto
{   
    public required string Username { get; set; }
    public string? NewUsername { get; set; }
    public string? Email { get; set; }
    public string? NewEmail { get; set; }
    public bool IsEnabled { get; set; }
}