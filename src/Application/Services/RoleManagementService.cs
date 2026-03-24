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
        var user = await _keycloakClientUser.GetUserByNameAsync(username);
        if (user == null)
        {
            return _errors.Fail<List<RoleProfileDto>>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }
        var userRoles = await _keycloakClientRole.GetUserRolesAsync(user.Id);
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

    public async Task<Result<List<UserProfileDto>>> GetUsersByRoleAsync(List<RoleDto> roles)
    {
        if (roles == null || roles.Count == 0)
            return _errors.Fail<List<UserProfileDto>>(ErrorCodes.AUTH.RolesNotFound);

        var allUsers = new Dictionary<string, UserProfileDto>();

        foreach (var roleDto in roles)
        {
            var role = await _keycloakClientRole.GetRoleByNameAsync(roleDto.RoleName);
            if (role == null)
                continue;

            var keycloakUsers = await _keycloakClientRole.GetUsersByRoleAsync(roleDto.RoleName);
            if (keycloakUsers == null)
                continue;

            var roleProfile = new RoleProfileDto
            {
                Id = role.Id,
                RoleName = role.Name,
                Description = role.Description
            };

            foreach (var u in keycloakUsers)
            {
                if (!allUsers.ContainsKey(u.Id))
                {
                    allUsers[u.Id] = new UserProfileDto
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        EmailVerified = u.EmailVerified,
                        Enabled = u.Enabled,
                        CreatedAt = u.CreatedAt,
                        Attributes = u.Attributes,
                        Roles = new List<RoleProfileDto> { roleProfile }
                    };
                }
            }
        }

        if (allUsers.Count == 0)
        {
            return _errors.Fail<List<UserProfileDto>>(ErrorCodes.AUTH.UsersNotFound);
        }

        return Result<List<UserProfileDto>>.Ok(allUsers.Values.ToList());
    }

    public async Task<Result<RoleProfileDto>> CreateRoleAsync(RoleDto roleDto)
    {
        // Check if role exists
        var role = await _keycloakClientRole.GetRoleByNameAsync(roleDto.RoleName);
        if (role != null)
        {
            return _errors.Fail<RoleProfileDto>(ErrorCodes.AUTH.RoleAlreadyExists);
        }

        // Create Role in Keycloak
        var keycloakRole = await _keycloakClientRole.CreateRoleAsync(roleDto.RoleName, roleDto.Description ?? string.Empty);
        if (keycloakRole == null)
        {
            return _errors.Fail<RoleProfileDto>(ErrorCodes.AUTH.CreateRoleFailed);
        }

        var newRole = await _keycloakClientRole.GetRoleByNameAsync(roleDto.RoleName);
        if (newRole == null)
        {
            return _errors.Fail<RoleProfileDto>(ErrorCodes.AUTH.CreateRoleFailed);
        }

        var createdRoleDto = new RoleProfileDto
        {
            Id = newRole.Id,
            RoleName = newRole.Name,
            Description = newRole.Description
        };

        return Result<RoleProfileDto>.Ok(createdRoleDto, $"Role {roleDto.RoleName} created successfully");
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

    public async Task<Result<bool>> DeleteRoleAsync(List<RoleDto> rolesToDelete)
    {
        if (rolesToDelete == null || rolesToDelete.Count == 0)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.RolesNotFound);
        }

        var failedRoles = new List<string>();

        foreach (var roleDto in rolesToDelete)
        {
            var role = await _keycloakClientRole.GetRoleByNameAsync(roleDto.RoleName);
            if (role == null)
            {
                failedRoles.Add(roleDto.RoleName);
                continue;
            }

            var result = await _keycloakClientRole.DeleteRoleAsync(roleDto.RoleName);
            if (!result)
            {
                return _errors.Fail<bool>(ErrorCodes.AUTH.DeleteRoleFailed);
            }
        }

        if (failedRoles.Count > 0)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.RoleNotFound);
        }

        return Result<bool>.Ok(data: true, message: "Roles deleted successfully.");
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
