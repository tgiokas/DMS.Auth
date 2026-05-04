namespace Authentication.Application.Dtos;

public class UserCreateDto
{
    public required string Username { get; set; }
    public string? Password { get; set; }
    public bool PasswordTemp { get; set; }
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool Enabled { get; set; }
    public bool IsAdmin { get; set; }
    public bool EmailVerified { get; set; }
}
