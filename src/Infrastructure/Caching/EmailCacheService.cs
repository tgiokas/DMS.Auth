using Microsoft.Extensions.Caching.Memory;

using Authentication.Application.Interfaces;

namespace Authentication.Infrastructure.Caching;

public class EmailCacheService : IEmailCacheService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(15);

    public EmailCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    private static string GetKey(string token) => $"email:verify:{token}";

    public void StoreToken(string token, string email, TimeSpan? ttl = null)
    {
        _cache.Set(GetKey(token), email, ttl ?? _defaultTtl);
    }

    public string? GetEmailByToken(string token)
    {
        return _cache.TryGetValue(GetKey(token), out string? email) ? email : null;
    }

    public void RemoveToken(string token)
    {
        _cache.Remove(GetKey(token));
    }
}
