using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IKeycloakClientRole
{
    Task<List<KeycloakRole>?> GetRolesAsync();
    Task<KeycloakRole?> GetRoleByNameAsync(string rolename);
    Task<KeycloakRole?> GetRoleByIdAsync(string roleId);
    Task<List<KeycloakRole>?> GetUserRolesAsync(string username);
    Task<KeycloakRole?> CreateRoleAsync(string roleName, string roleDescr);
    Task<KeycloakRole?> UpdateRoleAsync(RoleUpdateDto roleDto);
    Task<bool> DeleteRoleAsync(string roleName);
    Task<bool> AssignRoleAsync(string userId, string roleId, string rolename);
    Task<bool> RemoveRoleAsync(string userId, string roleId, string roleName);
}