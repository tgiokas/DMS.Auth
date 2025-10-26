using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface ITotpCache
{
    // Cached TOTP Secrets
    Task StoreSecretAsync(string token, TotpSecretCached entry, TimeSpan? ttl = null);
    Task<TotpSecretCached?> GetSecretAsync(string token);
    Task RemoveSecretAsync(string token);

    // Cached Login Attempts
    Task StoreLoginAttemptAsync(string token, LoginAttemptCached attempt, TimeSpan? ttl = null);
    Task<LoginAttemptCached?> GetLoginAttemptAsync(string token);
    Task RemoveLoginAttemptAsync(string token);
}
