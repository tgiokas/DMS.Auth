using DMS.Auth.Application.Dtos;

namespace DMS.Auth.Application.Interfaces;

public interface IUserManagementService
{
    Task<List<KeycloakUser>> GetUsersAsync();
    Task<bool> CreateUserAsync(CreateUserRequest request);
    Task<bool> AssignRoleAsync(string username, string roleId);
    Task<bool> EnableMfaAsync(string username);
    Task<bool> DeleteUserAsync(string username);
    Task<List<KeycloakRole>> GetUserRolesAsync(string username);
}
