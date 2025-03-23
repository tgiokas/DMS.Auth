using Microsoft.Extensions.Caching.Memory;
using DMS.Auth.Application.Interfaces;

namespace DMS.Auth.Infrastructure.Caching;

public class TotpInMemoryCacheService : ITotpCacheService
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    public TotpInMemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void StoreSecret(string username, string secret, TimeSpan? ttl = null)
    {
        _cache.Set(username, secret, ttl ?? DefaultTtl);
    }

    public string? GetSecret(string username)
    {
        _cache.TryGetValue(username, out string? secret);
        return secret;
    }

    public void RemoveSecret(string username)
    {
        _cache.Remove(username);
    }
}
