using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IRoleManagementService
{
    Task<Result<List<RoleProfileDto>>> GetRolesAsync();
    Task<Result<RoleProfileDto>> GetRoleByNameAsync(string rolename);
    Task<Result<RoleProfileDto>> GetRoleByIdAsync(string roleId);
    Task<Result<List<RoleProfileDto>>> GetUserRolesAsync(string username);
    Task<Result<List<UserProfileDto>>> GetUsersByRoleAsync(List<RoleDto> roles);
    Task<Result<RoleProfileDto>> CreateRoleAsync(RoleDto roleDto);
    Task<Result<RoleProfileDto>> UpdateRoleAsync(RoleUpdateDto roleDto);
    Task<Result<bool>> DeleteRoleAsync(List<RoleDto> rolesToDelete);
    Task<Result<bool>> AssignRolesToUserAsync(string username, List<RoleDto> rolesToAssign);
    Task<Result<bool>> RemoveRolesFromUserAsync(string username, List<RoleDto> rolesToRemove);
}
