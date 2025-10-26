using Microsoft.EntityFrameworkCore;

using Authentication.Domain.Entities;
using Authentication.Domain.Interfaces;
using Authentication.Infrastructure.Database;

namespace Authentication.Infrastructure.Repositories;

public class BusinessRuleRepository : IBusinessRuleRepository
{
    private readonly ApplicationDbContext _dbContext;

    public BusinessRuleRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<BusinessRule>> GetAllAsync()
    {
        return await _dbContext.BusinessRules.ToListAsync();
    }

    public async Task<BusinessRule?> GetByIdAsync(int id)
    {
        return await _dbContext.BusinessRules.FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<BusinessRule>> GetByDepartmentRoleAndMethodAsync(int departmentId, Guid roleId, string httpMethod)
    {
        return await _dbContext.BusinessRules
            .AsNoTracking()
            .Where(r => r.DepartmentId == departmentId
                && r.KeycloakRoleId == roleId
                && r.HttpMethod == httpMethod)
            .ToListAsync();
    }

    public async Task AddAsync(BusinessRule rule)
    {
        await _dbContext.BusinessRules.AddAsync(rule);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(BusinessRule rule)
    {
        _dbContext.BusinessRules.Update(rule);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(BusinessRule rule)
    {
        _dbContext.BusinessRules.Remove(rule);
        await _dbContext.SaveChangesAsync();
    }
}