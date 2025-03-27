using Microsoft.Extensions.Caching.Memory;
using DMS.Auth.Application.Interfaces;
using DMS.Auth.Application.Dtos;

public class TotpCacheService : ITotpCacheService
{
    private readonly IMemoryCache _cache;

    public TotpCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void StoreSecret(string token, TotpSecretCached entry, TimeSpan? ttl = null)
    {
        _cache.Set($"totp:{token}", entry, ttl ?? TimeSpan.FromMinutes(5));
    }

    public TotpSecretCached? GetSecret(string token)
    {
        _cache.TryGetValue($"totp:{token}", out TotpSecretCached? entry);
        return entry;
    }

    public void RemoveSecret(string token)
    {
        _cache.Remove($"totp:{token}");
    }

    public void StoreLoginAttempt(string token, LoginAttemptCached attempt, TimeSpan? ttl = null)
    {
        _cache.Set($"login:{token}", attempt, ttl ?? TimeSpan.FromMinutes(5));
    }

    public LoginAttemptCached? GetLoginAttempt(string token)
    {
        _cache.TryGetValue($"login:{token}", out LoginAttemptCached? attempt);
        return attempt;
    }

    public void RemoveLoginAttempt(string token)
    {
        _cache.Remove($"login:{token}");
    }
}
