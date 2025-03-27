namespace DMS.Auth.Application.Dtos;

public class TotpSecretCached
{
    public string Username { get; set; } = default!;
    public string Secret { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
}