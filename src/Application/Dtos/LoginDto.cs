namespace DMS.Auth.Application.Dtos;

public class LoginDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public required string Password { get; set; }
}
