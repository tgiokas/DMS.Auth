using Authentication.Domain.Entities;

namespace Authentication.Domain.Interfaces;

public interface IRolePermissionRepo
{
    Task<List<RolePermission>> GetAllAsync();
    Task<RolePermission?> GetByIdAsync(int id);
    Task<List<RolePermission>> GetByRoleIdAsync(Guid roleId);
    Task<bool> IsEndpointAuthorizedAsync(Guid roleId, string httpMethod, string path);
    Task AddAsync(RolePermission rule);
    Task UpdateAsync(RolePermission rule);
    Task DeleteAsync(RolePermission rule);    
}