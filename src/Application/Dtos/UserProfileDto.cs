using Authentication.Domain.Enums;

namespace Authentication.Application.Dtos;

public class UserProfileDto
{
    // Keycloak user properties
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool? EmailVerified { get; set; }
    public bool? Enabled { get; set; }

    // Local user properties
    public bool IsAdmin { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneVerified { get; set; }
    public string? MfaMethod { get; set; }
    public DateTime? CreatedAt { get; set; }
}
