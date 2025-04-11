using Authentication.Domain.Entities;

namespace Authentication.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber);
    Task<User?> GetByEmailNumberAsync(string email);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
}
