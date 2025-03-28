namespace DMS.Auth.Application.Dtos;

public class TotpSecretCached
{
    public required string Username { get; set; }
    public required string Secret { get; set; }
    public DateTime ExpiresAt { get; set; }
}