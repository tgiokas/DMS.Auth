using Microsoft.EntityFrameworkCore;

using Authentication.Domain.Entities;
using Authentication.Domain.Enums;
using Authentication.Domain.Interfaces;
using Authentication.Infrastructure.Database;

namespace Authentication.Infrastructure.Repositories;

public class ConfigurationRepository : IConfigurationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ConfigurationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Configuration?> GetConfigurationAsync()
    {
        return await _dbContext.Configurations.AsNoTracking().FirstOrDefaultAsync();
    }

    public async Task<MfaType> GetMfaTypeAsync()
    {
        var config = await _dbContext.Configurations.AsNoTracking().FirstOrDefaultAsync();
        return config?.MfaType ?? MfaType.None;
    }

    public async Task UpdateAsync(Configuration configuration)
    {
        _dbContext.Configurations.Update(configuration);
        await _dbContext.SaveChangesAsync();
    }    
}