using Authentication.Domain.Enums;

namespace Authentication.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public required Guid KeycloakUserId { get; set; }
    public required string Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsAdmin { get; set; }   
    public MfaType MfaType { get; set; } = MfaType.None;
    public bool PhoneVerified { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
}
