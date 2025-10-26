namespace Authentication.Application.Dtos;

public class LoginAttemptCached
{
    public required Guid KeycloakUserId { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public string? PhoneNumber { get; set; }  // Optional for SMS-based MFA
    public string? Email { get; set; }       // Optional for Email-based MFA    
}