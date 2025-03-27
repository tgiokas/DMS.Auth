namespace DMS.Auth.Domain.Interfaces;

public interface ITotpRepository
{
    Task SaveAsync(string userId, string base32Secret);
    Task<string?> GetAsync(string userId);
    Task<bool> ExistsAsync(string userId);
}
