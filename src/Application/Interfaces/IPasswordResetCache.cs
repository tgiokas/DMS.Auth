using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IPasswordResetCache
{
    Task StoreTokenAsync(string token, PasswordResetCached entry, TimeSpan? ttl = null);
    Task<PasswordResetCached?> GetTokenAsync(string token);
    Task RemoveTokenAsync(string token);
}