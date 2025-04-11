using Authentication.Domain.Entities;

namespace Authentication.Domain.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id);
    Task<Role?> GetByNameAsync(string username);
    Task AddAsync(Role user);
    Task UpdateAsync(Role user);
    Task DeleteAsync(Role user);
}
