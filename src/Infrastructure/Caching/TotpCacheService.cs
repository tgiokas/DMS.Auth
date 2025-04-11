using Microsoft.Extensions.Caching.Memory;

using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;

namespace Authentication.Infrastructure.Caching;

public class TotpCacheService : ITotpCacheService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(15);

    public TotpCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    private static string GetKey(string prefix, string token) => $"{prefix}:{token}";

    public void StoreSecret(string token, TotpSecretCached entry, TimeSpan? ttl = null)
    {
        _cache.Set(GetKey("totp", token), entry, ttl ?? _defaultTtl);
    }

    public TotpSecretCached? GetSecret(string token)
    {
        _cache.TryGetValue(GetKey("totp", token), out TotpSecretCached? entry);
        return entry;
    }

    public void RemoveSecret(string token)
    {
        _cache.Remove(GetKey("totp", token));
    }

    public void StoreLoginAttempt(string token, LoginAttemptCached attempt, TimeSpan? ttl = null)
    {
        _cache.Set(GetKey("login", token), attempt, ttl ?? _defaultTtl);
    }

    public LoginAttemptCached? GetLoginAttempt(string token)
    {
        _cache.TryGetValue(GetKey("login", token), out LoginAttemptCached? attempt);
        return attempt;
    }

    public void RemoveLoginAttempt(string token)
    {
        _cache.Remove(GetKey("login", token));
    }
}
