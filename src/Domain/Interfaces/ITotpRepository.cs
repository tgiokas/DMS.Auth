using Authentication.Domain.Entities;

namespace Authentication.Domain.Interfaces;

public interface ITotpRepository
{
    Task<string?> GetAsync(Guid keycloakUserId);
    Task<UserTotpSecret?> GetByUserIdAsync(Guid keycloakUserId);
    Task<bool> ExistsAsync(Guid keycloakUserId);
    Task AddAsync(UserTotpSecret userTotpSecret);
    Task UpdateAsync(UserTotpSecret userTotpSecret);
    Task DeleteAsync(Guid keycloakUserId);
}
