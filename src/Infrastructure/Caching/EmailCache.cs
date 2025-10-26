using Microsoft.Extensions.Caching.Distributed;

using Authentication.Application.Interfaces;
using Authentication.Infrastructure.Constants;

namespace Authentication.Infrastructure.Caching;

public class EmailCache : IEmailCache
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(CacheConstants.EmailCacheTtlMins);
    
    public EmailCache(IDistributedCache cache)
    {
        _cache = cache;
    }

    private static string GetKey(string prefix, string token) => $"{prefix}:{token}";

    // Email verification
    public async Task StoreTokenAsync(string token, string email, TimeSpan? ttl = null)
    {
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl ?? _defaultTtl };
        await _cache.SetStringAsync(GetKey("email:verify", token), email, options);
    }

    public async Task<string?> GetEmailByTokenAsync(string token)
    {
        return await _cache.GetStringAsync(GetKey("email:verify", token));
    }

    public async Task RemoveTokenAsync(string token)
    {
        await _cache.RemoveAsync(GetKey("email:verify", token));
    }

    // MFA Codes
    public async Task StoreCodeAsync(string email, string code, TimeSpan? ttl = null)
    {
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl ?? _defaultTtl };
        await _cache.SetStringAsync(GetKey("email:mfa", email), code, options);
    }

    public async Task<string?> GetCodeAsync(string email)
    {
        return await _cache.GetStringAsync(GetKey("email:mfa", email));
    }

    public async Task RemoveCodeAsync(string email)
    {
        await _cache.RemoveAsync(GetKey("email:mfa", email));
    }
}
