using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;
using Authentication.Infrastructure.Constants;

namespace Authentication.Infrastructure.Caching;

public class PasswordResetCache : IPasswordResetCache
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(CacheConstants.PasswordResetCacheTtlMins);

    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PasswordResetCache(IDistributedCache cache)
    {
        _cache = cache;
    }

    private static string GetKey(string token)=> $"reset:pw:{token}";   

    public async Task StoreTokenAsync(string token, PasswordResetCached entry, TimeSpan? ttl = null)
    {
        var json = JsonSerializer.Serialize(entry, _jsonOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? _defaultTtl
        };

        await _cache.SetStringAsync(GetKey(token), json, options);
    }

    public async Task<PasswordResetCached?> GetTokenAsync(string token)
    {
        var json = await _cache.GetStringAsync(GetKey(token));
        if (json == null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<PasswordResetCached>(json, _jsonOptions);
    }

    public async Task RemoveTokenAsync(string token)
    {
        await _cache.RemoveAsync(GetKey(token));
    }
}
