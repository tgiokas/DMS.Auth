using Authentication.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Authentication.Infrastructure.Caching;

public class SmsCacheService : ISmsCacheService
{
    private readonly IMemoryCache _cache;

    public SmsCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void StoreCode(string token, string entry, TimeSpan? ttl = null)
    {
        _cache.Set($"sms:verify:{token}", entry, ttl ?? TimeSpan.FromMinutes(5));
    }

    public string? GetCode(string token)
    {
        _cache.TryGetValue($"sms:verify:{token}", out string? entry);
        return entry;
    }

    public void RemoveCode(string token)
    {
        _cache.Remove($"sms:verify:{token}");
    }
}
