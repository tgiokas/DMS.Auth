using Authentication.Domain.Entities;

namespace Authentication.Domain.Interfaces;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync();
    Task<List<User>> GetNotDeletedAsync();
    Task<User?> GetByKeycloakUserIdAsync(Guid keycloakUserId);
    Task<User?> GetByUsernameAsync(string username);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
}
