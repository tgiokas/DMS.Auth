using Authentication.Domain.Enums;

namespace Authentication.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public required Guid KeycloakUserId { get; set; }
    public required string Username { get; set; }
    public string? PhoneNumber { get; set; }
    public MfaType MfaType { get; set; } = MfaType.None;
    public bool IsAdmin { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
}
