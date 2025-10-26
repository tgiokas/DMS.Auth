namespace Authentication.Domain.Entities;

public class UserTotpSecret
{
    public int Id { get; set; }
    public Guid KeycloakUserId { get; set; }
    public string Base32Secret { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool Verified { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastVerifiedAt { get; set; }    
}
