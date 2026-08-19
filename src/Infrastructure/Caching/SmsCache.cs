using Authentication.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Authentication.Infrastructure.Caching;

public class SmsCache : ISmsCache
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(5);

    public SmsCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    private static string GetKey(string token) => $"sms:verify:{token}";
    private static string GetMfaLoginKey(string token) => $"sms:mfa:login:{token}";

    public void StoreCode(string token, string entry, TimeSpan? ttl = null)
    {
        _cache.Set(GetKey(token), entry, ttl ?? _defaultTtl);
    }

    public string? GetCode(string token)
    {
        _cache.TryGetValue(GetKey(token), out string? entry);
        return entry;
    }

    public void RemoveCode(string token)
    {
        _cache.Remove(GetKey(token));
    }

    public void StoreMfaLoginCode(string token, string entry, TimeSpan? ttl = null)
    {
        _cache.Set(GetMfaLoginKey(token), entry, ttl ?? _defaultTtl);
    }

    public string? GetMfaLoginCode(string token)
    {
        _cache.TryGetValue(GetMfaLoginKey(token), out string? entry);
        return entry;
    }

    public void RemoveMfaLoginCode(string token)
    {
        _cache.Remove(GetMfaLoginKey(token));
    }
}
