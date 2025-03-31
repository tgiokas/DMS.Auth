using Microsoft.EntityFrameworkCore;

using DMS.Auth.Domain.Entities;
using DMS.Auth.Domain.Interfaces;
using DMS.Auth.Infrastructure.Database;

namespace DMS.Auth.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RoleRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Role?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id);
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

    public async Task DeleteAsync(Role role)
    {
        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync();
    }
}