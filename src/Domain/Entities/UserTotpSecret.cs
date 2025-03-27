namespace DMS.Auth.Domain.Entities;

public class UserTotpSecret
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Base32Secret { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastVerifiedAt { get; set; }
    public bool Verified { get; set; }

    public UserTotpSecret() { }
}
