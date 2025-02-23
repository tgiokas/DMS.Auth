using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;
using Microsoft.Extensions.Logging;

public class UserManagementService
{
    private readonly IKeycloakClient _keycloakClient;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(IKeycloakClient keycloakClient, ILogger<UserManagementService> logger)
    {
        _keycloakClient = keycloakClient;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all users from Keycloak.
    /// </summary>
    public async Task<List<KeycloakUser>> GetUsersAsync()
    {
        return await _keycloakClient.GetUsersAsync();
    }

    /// <summary>
    /// Creates a new user in Keycloak.
    /// </summary>
    public async Task<bool> CreateUserAsync(CreateUserRequest request)
    {
        return await _keycloakClient.CreateUserAsync(request.Username, request.Email, request.Password);        
    }

    public async Task<bool> UpdateUserAsync(UpdateUserRequest request)
    {
        return await _keycloakClient.UpdateUserAsync(request);
    }

    /// <summary>
    /// Assigns a role to a user in Keycloak.
    /// </summary>
    public async Task<bool> AssignRoleAsync(string username, string roleId)
    {
        return await _keycloakClient.AssignRoleAsync(username, roleId);
    }

    /// <summary>
    /// Enables Multi-Factor Authentication (MFA) for a user.
    /// </summary>
    public async Task<bool> EnableMfaAsync(string username)
    {
        return await _keycloakClient.EnableMfaAsync(username);
    }

    /// <summary>
    /// Deletes a user from Keycloak.
    /// </summary>
    public async Task<bool> DeleteUserAsync(string username)
    {
        return await _keycloakClient.DeleteUserAsync(username);
    }

    /// <summary>
    /// Retrieves all roles assigned to a user.
    /// </summary>
    public async Task<List<KeycloakRole>> GetUserRolesAsync(string username)
    {
        return await _keycloakClient.GetUserRolesAsync(username);
    }
}
