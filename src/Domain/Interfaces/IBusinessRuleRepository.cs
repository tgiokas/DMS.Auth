using Authentication.Domain.Entities;

namespace Authentication.Domain.Interfaces;

public interface IBusinessRuleRepository
{
    Task<List<BusinessRule>> GetAllAsync();
    Task<BusinessRule?> GetByIdAsync(int id);
    Task<List<BusinessRule>> GetByDepartmentRoleAndMethodAsync(int departmentId, Guid roleId, string httpMethod);
    Task AddAsync(BusinessRule rule);
    Task UpdateAsync(BusinessRule rule);
    Task DeleteAsync(BusinessRule rule);    
}