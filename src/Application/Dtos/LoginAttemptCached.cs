namespace DMS.Auth.Application.Dtos;

public class LoginAttemptCached
{
    public required string UserId { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }    
    public DateTime ExpiresAt { get; set; }
}