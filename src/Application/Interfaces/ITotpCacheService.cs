using DMS.Auth.Application.Dtos;

namespace DMS.Auth.Application.Interfaces;

public interface ITotpCacheService
{
    // Cached TOTP Secrets
    void StoreSecret(string token, TotpSecretCached entry, TimeSpan? ttl = null);
    TotpSecretCached? GetSecret(string token);
    void RemoveSecret(string token);

    // Cached Login Attempts
    void StoreLoginAttempt(string token, LoginAttemptCached attempt, TimeSpan? ttl = null);
    LoginAttemptCached? GetLoginAttempt(string token);
    void RemoveLoginAttempt(string token);
}
