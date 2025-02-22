using DMS.Auth.Application.Dtos;

namespace DMS.Auth.Application.Interfaces;

public interface IAuthenticationService
{
    Task<UserDto> CreateUserAsync(CreateUserRequest request);

    Task EnableMfaAsync(Guid userId);

    Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task DeleteUserAsync(Guid userId);

    Task AssignRoleAsync(Guid userId, string roleName);
}

