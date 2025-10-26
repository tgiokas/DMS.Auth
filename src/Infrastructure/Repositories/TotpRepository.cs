using Microsoft.EntityFrameworkCore;

using Authentication.Domain.Entities;
using Authentication.Domain.Interfaces;
using Authentication.Infrastructure.Database;

namespace Authentication.Infrastructure.Repositories;

public class TotpRepository : ITotpRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TotpRepository(ApplicationDbContext context)
    {
        _dbContext = context;
    }

    public async Task<string?> GetAsync(Guid keycloakUserId)
    {
        return await _dbContext.UserTotpSecrets
            .AsNoTracking()
            .Where(x => x.KeycloakUserId == keycloakUserId)
            .Select(x => x.Base32Secret)
            .FirstOrDefaultAsync();
    }

    public async Task<UserTotpSecret?> GetByUserIdAsync(Guid keycloakUserId)
    {
        return await _dbContext.UserTotpSecrets
            .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId);
    }

    public async Task<bool> ExistsAsync(Guid keycloakUserId)
    {
        return await _dbContext.UserTotpSecrets
            .AsNoTracking()
            .AnyAsync(x => x.KeycloakUserId == keycloakUserId);
    }

    public async Task AddAsync(UserTotpSecret userTotpSecret)
    {
        var existing = await _dbContext.UserTotpSecrets
            .FirstOrDefaultAsync(x => x.KeycloakUserId == userTotpSecret.KeycloakUserId);

        if (existing is null)
        {
            await _dbContext.UserTotpSecrets.AddAsync(userTotpSecret);            
            await _dbContext.SaveChangesAsync();            
        }        
    }

    public async Task UpdateAsync(UserTotpSecret userTotpSecret)
    {
        _dbContext.UserTotpSecrets.Update(userTotpSecret);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid keycloakUserId)
    {
        var entity = await _dbContext.UserTotpSecrets
            .FirstOrDefaultAsync(x => x.KeycloakUserId == keycloakUserId);

        if (entity != null)
        {
            _dbContext.UserTotpSecrets.Remove(entity);
            await _dbContext.SaveChangesAsync();
        }
    }
}
