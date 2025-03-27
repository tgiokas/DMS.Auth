﻿using Microsoft.EntityFrameworkCore;

using DMS.Auth.Domain.Entities;
using DMS.Auth.Domain.Interfaces;
using DMS.Auth.Infrastructure.Persistence;

namespace DMS.Auth.Infrastructure.Repositories;

public class TotpSecretRepository : ITotpRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TotpSecretRepository(ApplicationDbContext context)
    {
        _dbContext = context;
    }

    public async Task SaveAsync(string userId, string base32Secret)
    {
        try
        {
            var existing = await _dbContext.UserTotpSecrets.FirstOrDefaultAsync(x => x.UserId == userId);

            if (existing is null)
            {
                await _dbContext.UserTotpSecrets.AddAsync(new UserTotpSecret
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Base32Secret = base32Secret
                });
            }
            else
            {
                existing.Base32Secret = base32Secret;
            }

            await _dbContext.SaveChangesAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<string?> GetAsync(string userId)
    {
        try
        {
            return await _dbContext.UserTotpSecrets
            .Where(x => x.UserId == userId)
            .Select(x => x.Base32Secret)
            .FirstOrDefaultAsync();
        }
        catch (Exception)
        {
            return null;
        }        
    }

    public async Task<bool> ExistsAsync(string userId)
    {
        return await _dbContext.UserTotpSecrets.AnyAsync(x => x.UserId == userId);
    }
}
