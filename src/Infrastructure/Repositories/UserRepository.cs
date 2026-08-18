using Microsoft.EntityFrameworkCore;

using Authentication.Domain.Entities;
using Authentication.Domain.Interfaces;
using Authentication.Infrastructure.Database;

namespace Authentication.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _dbContext.Users.ToListAsync();
    }

    public async Task<List<User>> GetNotDeletedAsync()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(r => r.IsDeleted == false)              
            .ToListAsync();
    }

    public async Task<List<Guid>> GetNotDeletedAsync(List<Guid> keycloakUserIds)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(r => keycloakUserIds.Contains(r.KeycloakUserId) && !r.IsDeleted)
            .Select(s => s.KeycloakUserId)
            .ToListAsync();
    }

    public async Task<User?> GetByKeycloakUserIdAsync(Guid keycloakUserId)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<List<(Guid KeycloakUserId, bool IsDeleted)>> AreDeletedAsync(List<Guid> keycloakUserIds)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => keycloakUserIds.Contains(u.KeycloakUserId))
            .Select(s => new ValueTuple<Guid, bool>(s.KeycloakUserId, s.IsDeleted))
            .ToListAsync();
    }

    public async Task AddAsync(User user)
    {
        var existing = await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == user.Username);

        if (existing is null)
        {
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
        }        
    }

    public async Task UpdateAsync(User user)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
    }    
}