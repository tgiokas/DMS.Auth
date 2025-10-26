namespace Authentication.Application.Dtos;

public class KeycloakUserDto
{
    public string? Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public bool PasswordTemp { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }    
    public bool? Enabled { get; set; }    
    public bool EmailVerified { get; set; }
}