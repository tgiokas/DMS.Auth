namespace Authentication.Domain.Interfaces;

public interface ITotpRepository
{
    Task AddAsync(string userId, string base32Secret);
    Task<string?> GetAsync(string userId);
    Task<bool> ExistsAsync(string userId);
}
