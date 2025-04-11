using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IPasswordForgotCacheService
{
    void StoreToken(string token, PasswordForgotTokenEntry entry, TimeSpan? ttl = null);
    PasswordForgotTokenEntry? GetToken(string token);
    void RemoveToken(string token);
}