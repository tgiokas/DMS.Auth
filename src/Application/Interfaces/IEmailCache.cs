namespace Authentication.Application.Interfaces;

public interface IEmailCache
{
    // Email account verification
    Task StoreTokenAsync(string token, string email, TimeSpan? ttl = null);
    Task<string?> GetEmailByTokenAsync(string token);
    Task RemoveTokenAsync(string token);

    // MFA Code
    Task StoreCodeAsync(string email, string code, TimeSpan? ttl = null);
    Task<string?> GetCodeAsync(string email);
    Task RemoveCodeAsync(string email);
}

