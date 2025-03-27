namespace DMS.Auth.Application.Dtos;

public class LoginAttemptCached
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
}