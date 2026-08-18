namespace Authentication.Application.Dtos;

public class LoginAttemptCached
{
    public required Guid KeycloakUserId { get; set; }
    public required string Username { get; set; }
    public string? PhoneNumber { get; set; }  // Optional for SMS-based MFA
    public string? Email { get; set; }       // Optional for Email-based MFA

    // Password-step lockout key, cleared only once MFA succeeds
    public required string LoginKey { get; set; }

    // Tokens obtained after validating username/password(store short-lived)
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required int ExpiresIn { get; set; }
    public bool RememberMe { get; set; }

    // Resend throttling for send-email / send-sms
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int ResendCount { get; set; }
    public DateTime? LastResendUtc { get; set; }
}
