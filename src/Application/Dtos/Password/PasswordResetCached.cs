namespace Authentication.Application.Dtos;

public class PasswordResetCached
{
    public string Email { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}
