namespace Authentication.Application.Interfaces;

public interface IEmailCacheService
{
    void StoreToken(string token, string email, TimeSpan? ttl = null);
    string? GetEmailByToken(string token);
    void RemoveToken(string token);
}
