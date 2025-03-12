using DMS.Auth.Domain.Entities;

namespace DMS.Auth.Domain.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id);
    Task<Role?> GetByNameAsync(string username, string agencyId);
    Task AddAsync(Role user);
    Task UpdateAsync(Role user);
    Task DeleteAsync(Role user);
}
