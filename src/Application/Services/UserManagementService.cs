using Microsoft.Extensions.Logging;

using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;

namespace DMS.Auth.Application.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IKeycloakClient _keycloakClient;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(IKeycloakClient keycloakClient, ILogger<UserManagementService> logger)
    {
        _keycloakClient = keycloakClient;
        _logger = logger;
    }

    public async Task<List<KeycloakUser>> GetUsersAsync()
    {
        return await _keycloakClient.GetUsersAsync();
    }

    public async Task<bool> CreateUserAsync(CreateUserDto request)
    {
        return await _keycloakClient.CreateUserAsync(request.Username, request.Email, request.Password);        
    }

    public async Task<bool> UpdateUserAsync(UpdateUserDto request)
    {
        return await _keycloakClient.UpdateUserAsync(request);
    }

    public async Task<bool> AssignRoleAsync(string username, string roleId)
    {
        return await _keycloakClient.AssignRoleAsync(username, roleId);
    }

    public async Task<bool> EnableMfaAsync(string username)
    {
        return await _keycloakClient.EnableMfaAsync(username);
    }

    public async Task<bool> DeleteUserAsync(string username)
    {
        return await _keycloakClient.DeleteUserAsync(username);
    }

    public async Task<List<KeycloakRole>> GetUserRolesAsync(string username)
    {
        return await _keycloakClient.GetUserRolesAsync(username);
    }
}
