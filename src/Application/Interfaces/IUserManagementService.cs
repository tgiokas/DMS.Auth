using DMS.Auth.Application.Dtos;

namespace DMS.Auth.Application.Interfaces;

public interface IUserManagementService
{
    Task<List<KeycloakUser>> GetUsersAsync();
    Task<bool> CreateUserAsync(CreateUserDto request);
    Task<bool> UpdateUserAsync(UpdateUserDto request);
    Task<bool> DeleteUserAsync(string username);
    Task<List<KeycloakRole>> GetUserRolesAsync(string username);
    Task<bool> AssignRoleAsync(string username, string roleId);
    Task<bool> EnableMfaAsync(string username);
}
