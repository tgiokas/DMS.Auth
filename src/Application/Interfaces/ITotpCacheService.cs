namespace DMS.Auth.Application.Interfaces;

public interface ITotpCacheService
{
    void StoreSecret(string username, string secret, TimeSpan? ttl = null);
    string? GetSecret(string username);
    void RemoveSecret(string username);
}
