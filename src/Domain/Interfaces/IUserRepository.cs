using Authentication.Domain.Entities;

namespace Authentication.Domain.Interfaces;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync();
    Task<User?> GetByKeycloakUserIdAsync(Guid keycloakUserId);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber);
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
}
