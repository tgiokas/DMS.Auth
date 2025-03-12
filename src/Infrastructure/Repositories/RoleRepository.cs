using Microsoft.EntityFrameworkCore;

using DMS.Auth.Domain.Entities;
using DMS.Auth.Domain.Interfaces;
using DMS.Auth.Infrastructure.Persistence;

namespace DMS.Auth.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RoleRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Role?> GetByNameAsync(string roleName)
    {
        return await _dbContext.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Name == roleName);
    }

    public async Task AddAsync(Role role)
    {
        await _dbContext.Roles.AddAsync(role);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Role role)
    {
        _dbContext.Roles.Update(role);
        await _dbContext.SaveChangesAsync();
    }

    public Task<Role?> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<Role?> GetByNameAsync(string username, string agencyId)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Role user)
    {
        throw new NotImplementedException();
    }
}
