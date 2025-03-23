namespace DMS.Auth.Application.Dtos;

public class UserCreateDto
{
    public required string Username { get; set; }
    public string? Password { get; set; }
    public required string Email { get; set; }
    public bool? IsMfaEnabled { get; set; }
    public string? AgencyId { get; set; }
}
