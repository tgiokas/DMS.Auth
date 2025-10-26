using System.Data;

using Authentication.Application.Dtos;
using Authentication.Application.Errors;
using Authentication.Application.Interfaces;

namespace Authentication.Application.Services;

public class RoleManagementService : IRoleManagementService
{
    private readonly IKeycloakClientRole _keycloakClientRole;
    private readonly IKeycloakClientUser _keycloakClientUser;   
    private readonly IErrorCatalog _errors;

    public RoleManagementService(
        IKeycloakClientRole keycloakClientRole,
        IKeycloakClientUser keycloakClientUser,      
        IErrorCatalog errors)
    {
        _keycloakClientUser = keycloakClientUser;
        _keycloakClientRole = keycloakClientRole;       
        _errors = errors;
    }

    public async Task<Result<List<RoleProfileDto>>> GetRolesAsync()
    {
        var roles = await _keycloakClientRole.GetRolesAsync();
        if (roles == null)
        {
            return _errors.Fail<List<RoleProfileDto>>(ErrorCodes.AUTH.RolesNotFound);
        }

        var roleDtos = roles.Select(r => new RoleProfileDto
        {
            Id = r.Id,
            RoleName = r.Name,
            Description = r.Description
        }).ToList();

        return Result<List<RoleProfileDto>>.Ok(roleDtos);
    }

    public async Task<Result<RoleProfileDto>> GetRoleByNameAsync(string rolename)
    {
        var role = await _keycloakClientRole.GetRoleByNameAsync(rolename);
        if (role == null)
        {
            return _errors.Fail<RoleProfileDto>(ErrorCodes.AUTH.RoleNotFound);
        }

        var roleDto = new RoleProfileDto
        {
            Id = role.Id,
            RoleName = role.Name,
            Description = role.Description
        };

        return Result<RoleProfileDto>.Ok(roleDto);
    }

    public async Task<Result<RoleProfileDto>> GetRoleByIdAsync(string roleId)
    {
        var role = await _keycloakClientRole.GetRoleByIdAsync(roleId);
        if (role == null)
        {
            return _errors.Fail<RoleProfileDto>(ErrorCodes.AUTH.RoleNotFound);
        }

        var roleDto = new RoleProfileDto
        {
            Id = role.Id,
            RoleName = role.Name,
            Description = role.Description
        };

        return Result<RoleProfileDto>.Ok(roleDto);
    }

    public async Task<Result<List<RoleProfileDto>>> GetUserRolesAsync(string username)
    {
        var userId = await _keycloakClientUser.GetUserIdByUsernameAsync(username);
        if (userId == null)
        {
            return _errors.Fail<List<RoleProfileDto>>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }
        var userRoles = await _keycloakClientRole.GetUserRolesAsync(userId);
        if (userRoles == null)
        {
            return _errors.Fail<List<RoleProfileDto>>(ErrorCodes.AUTH.UserRolesNotFound);
        }

        var roleDtos = userRoles.Select(r => new RoleProfileDto
        {
            Id = r.Id,
            RoleName = r.Name,
            Description = r.Description
        }).ToList();

        return Result<List<RoleProfileDto>>.Ok(roleDtos);
    }

    public async Task<Result<RoleDto>> CreateRoleAsync(RoleDto roleDto)
    {
        // Check if role exists
        var role = await _keycloakClientRole.GetRoleByNameAsync(roleDto.RoleName);
        if (role != null)
        {
            return _errors.Fail<RoleDto>(ErrorCodes.AUTH.RoleAlreadyExists);
        }

        // Create Role in Keycloak
        var keycloakRole = await _keycloakClientRole.CreateRoleAsync(roleDto.RoleName, roleDto.Description ?? string.Empty);
        if (keycloakRole == null)
        {
            return _errors.Fail<RoleDto>(ErrorCodes.AUTH.CreateRoleFailed);
        }

        var createdRoleDto = new RoleDto
        {
            RoleName = keycloakRole.Name,
            Description = keycloakRole.Description
        };

        return Result<RoleDto>.Ok(createdRoleDto, $"Role {roleDto.RoleName} created successfully");
    }

    public async Task<Result<RoleProfileDto>> UpdateRoleAsync(RoleUpdateDto roleDto)
    {
        // Check if role exists
        var role = await _keycloakClientRole.GetRoleByNameAsync(roleDto.RoleName);
        if (role == null)
        {
            return _errors.Fail<RoleProfileDto>(ErrorCodes.AUTH.RoleNotFound);
        }

        // Update Role in Keycloak
        var keycloakRole = await _keycloakClientRole.UpdateRoleAsync(roleDto);
        if (keycloakRole == null)
        {
            return _errors.Fail<RoleProfileDto>(ErrorCodes.AUTH.UpdateRoleFailed);
        }

        var updatedRoleDto = new RoleProfileDto
        {
            Id = role.Id,
            RoleName = keycloakRole.Name,
            Description = keycloakRole.Description
        };

        return Result<RoleProfileDto>.Ok(updatedRoleDto, $"Role {roleDto.RoleName} updated successfully");
    }

    public async Task<Result<bool>> DeleteRoleAsync(string rolename)
    {
        // Check if role exists
        var role = await _keycloakClientRole.GetRoleByNameAsync(rolename);
        if (role == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.RoleNotFound);
        }

        // Delete role from Keycloak
        var result = await _keycloakClientRole.DeleteRoleAsync(rolename);
        if (!result)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.DeleteRoleFailed);
        }

        return Result<bool>.Ok(data: true, message: $"Role {rolename} deleted successfully.");
    }

    public async Task<Result<bool>> AssignRolesToUserAsync(string username, List<RoleDto> rolesToAssign)
    {
        if (string.IsNullOrWhiteSpace(username) || rolesToAssign == null || rolesToAssign.Count == 0)
            return _errors.Fail<bool>(ErrorCodes.AUTH.UsernameAndRolesRequired);

        var user = await _keycloakClientUser.GetUserByNameAsync(username);
        if (user == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        foreach (var roleToAssign in rolesToAssign)
        {
            var role = await _keycloakClientRole.GetRoleByNameAsync(roleToAssign.RoleName);
            if (role == null)
            {
                return _errors.Fail<bool>(ErrorCodes.AUTH.RoleNotFound);
            }

            var result = await _keycloakClientRole.AssignRoleAsync(user.Id, role.Id, role.Name);
            if (!result)
            {
                return _errors.Fail<bool>(ErrorCodes.AUTH.AssignRoleFailed);
            }
        }

        return Result<bool>.Ok(data: true, message: $"Roles assigned to user {username} successfully.");
    }

    public async Task<Result<bool>> RemoveRolesFromUserAsync(string username, List<RoleDto> rolesToRemove)
    {
        var user = await _keycloakClientUser.GetUserByNameAsync(username);
        if (user == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        foreach (var roleToRemove in rolesToRemove)
        {
            var role = await _keycloakClientRole.GetRoleByNameAsync(roleToRemove.RoleName);
            if (role == null)
            {
                return _errors.Fail<bool>(ErrorCodes.AUTH.RoleNotFound);
            }

            var result = await _keycloakClientRole.RemoveRoleAsync(user.Id, role.Id, role.Name);
            if (!result)
            {
                return _errors.Fail<bool>(ErrorCodes.AUTH.RemoveRoleFailed);
            }
        }

        return Result<bool>.Ok(data: true, message: $"Roles removed from user {username} successfully.");
    }
}
