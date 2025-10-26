namespace Authentication.Application.Dtos;

public class UserUpdateDto
{
    public required string Id { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? Enabled { get; set; }
    public bool? IsAdmin { get; set; }   
}
