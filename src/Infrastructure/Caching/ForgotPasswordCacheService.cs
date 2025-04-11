using Microsoft.Extensions.Caching.Memory;

using Authentication.Application.Interfaces;
using Authentication.Application.Dtos;

namespace Authentication.Infrastructure.Caching;

public class ForgotPasswordCacheService : IPasswordForgotCacheService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(15);

    public ForgotPasswordCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    private static string GetKey(string token) => $"reset:pw:{token}";

    public void StoreToken(string token, PasswordForgotTokenEntry entry, TimeSpan? ttl = null)
    {
        _cache.Set(GetKey(token), entry, ttl ?? _defaultTtl);
    }

    public PasswordForgotTokenEntry? GetToken(string token)
    {
        _cache.TryGetValue(GetKey(token), out PasswordForgotTokenEntry? entry);
        return entry;
    }

    public void RemoveToken(string token)
    {
        _cache.Remove(GetKey(token));
    }
}
