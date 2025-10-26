using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;
using Authentication.Infrastructure.Constants;

namespace Authentication.Infrastructure.Caching;

public class TotpCache : ITotpCache
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(CacheConstants.TotpCacheTtlMins);

    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TotpCache(IDistributedCache cache)
    {
        _cache = cache;
    }

    private static string GetKey(string prefix, string token) => $"{prefix}:{token}";

    // Cached TOTP Secrets
    public async Task StoreSecretAsync(string token, TotpSecretCached entry, TimeSpan? ttl = null)
    {
        var json = JsonSerializer.Serialize(entry, _jsonOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? _defaultTtl
        };

        await _cache.SetStringAsync(GetKey("totp", token), json, options);
    }

    public async Task<TotpSecretCached?> GetSecretAsync(string token)
    {
        var json = await _cache.GetStringAsync(GetKey("totp", token));
        if (json == null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<TotpSecretCached>(json, _jsonOptions);
    }

    public async Task RemoveSecretAsync(string token)
    {
        await _cache.RemoveAsync(GetKey("totp", token));
    }

    // Cached Login Attempts
    public async Task StoreLoginAttemptAsync(string token, LoginAttemptCached attempt, TimeSpan? ttl = null)
    {
        var json = JsonSerializer.Serialize(attempt, _jsonOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? _defaultTtl
        };

        await _cache.SetStringAsync(GetKey("login", token), json, options);
    }

    public async Task<LoginAttemptCached?> GetLoginAttemptAsync(string token)
    {
        var json = await _cache.GetStringAsync(GetKey("login", token));
        if (json == null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<LoginAttemptCached>(json, _jsonOptions);
    }

    public async Task RemoveLoginAttemptAsync(string token)
    {
        await _cache.RemoveAsync(GetKey("login", token));
    }
}
