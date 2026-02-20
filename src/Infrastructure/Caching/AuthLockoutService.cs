using Microsoft.Extensions.Caching.Distributed;
using Authentication.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Authentication.Infrastructure.Caching;

public sealed class AuthLockoutService : IAuthLockoutService
{
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;

    // Counts failures
    private readonly int _maxLoginFailures; 
    // Time window in which failed attempts are counted
    private readonly TimeSpan _failureResetTime;
    // Lock duration (Wait increment in keycloak) applied once MaxLoginFailures is exceeded
    private readonly TimeSpan _lockDuration;

    public AuthLockoutService(IDistributedCache cache, IConfiguration configuration)
    {
        _cache = cache;
        _configuration = configuration;
        _maxLoginFailures = int.Parse(configuration["AUTH_MAX_LOGIN_FAILURES"] ?? throw new ArgumentNullException(nameof(configuration), "AUTH_MAX_LOGIN_FAILURES is null."));
        _failureResetTime = TimeSpan.FromMinutes(int.Parse(configuration["AUTH_FAILURE_RESET_TIME_MINS"] ?? throw new ArgumentNullException(nameof(configuration), "AUTH_FAILURE_RESET_TIME_MINS is null.")));
        _lockDuration = TimeSpan.FromMinutes(int.Parse(configuration["AUTH_LOCK_DURATION_MINS"] ?? throw new ArgumentNullException(nameof(configuration), "AUTH_LOCK_DURATION_MINS is null.")));
    }

    private static string GetKey(string prefix, string token) => $"{prefix}:{token}";

    private const string AttemptsPrefix = "attempts";
    private const string LockPrefix = "lock";

    public async Task<bool> IsLockedAsync(string loginKey)
    {
        var locked = await _cache.GetStringAsync(GetKey(LockPrefix, loginKey));
        return locked is not null;
    }

    public async Task RegisterLoginFailureAsync(string loginKey)
    {
        // If already locked, don't keep incrementing
        if (await IsLockedAsync(loginKey))
            return;

        var attemptsKey = GetKey(AttemptsPrefix, loginKey);

        var currentStr = await _cache.GetStringAsync(attemptsKey);
        int current = int.TryParse(currentStr, out var value) ? value : 0;
        current++;

        // Store updated attempts with attempts window TTL
        await _cache.SetStringAsync(
            attemptsKey,
            current.ToString(),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _failureResetTime
            });

        if (current >= _maxLoginFailures)
        {
            // Set lock flag with lock TTL
            await _cache.SetStringAsync(
                GetKey(LockPrefix, loginKey),
                "1",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _lockDuration
                });

            // Reset attempts after locking
            await _cache.RemoveAsync(attemptsKey);
        }
    }

    public async Task RegisterLoginSuccessAsync(string loginKey)
    {
        // Reset on success
        await _cache.RemoveAsync(GetKey(AttemptsPrefix, loginKey));
        await _cache.RemoveAsync(GetKey(LockPrefix, loginKey));
    }
}