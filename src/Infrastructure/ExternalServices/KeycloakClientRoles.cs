using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;

namespace Authentication.Infrastructure.ExternalServices;

public partial class KeycloakClient : KeycloakApiClient, IKeycloakClient
{
    public async Task<List<KeycloakRole>?> GetUserRolesAsync(string username)
    {
        var userId = await GetUserIdByUsernameAsync(username);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("User {Username} not found in Keycloak", username);
            return null;
        }

        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/realm";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUrl);

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch roles for user {Username} in Keycloak: {Response}", username, await response.Content.ReadAsStringAsync());
            return null;
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var roles = JsonSerializer.Deserialize<List<KeycloakRole>>(jsonResponse);

        if (roles == null)
        {
            _logger.LogWarning("Empty response body while fetching users from Keycloak.");
            return null;
        }

        return roles;
    }

    public async Task<bool> CreateRoleAsync(string roleName, string roleDescr, string realm)
    {
        var newRole = new KeycloakRole
        {
            Name = roleName,
            Description = roleDescr,
            Composite = false,
            ClientRole = false,
            ContainerId = realm
        };

        var jsonPayload = JsonSerializer.Serialize(newRole);
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/roles";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, requestUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to create role in Keycloak: {Response}", await response.Content.ReadAsStringAsync());
            return false;
        }

        return true;
    }

    public async Task<bool> AssignRoleAsync(string username, string roleId)
    {
        var userId = await GetUserIdByUsernameAsync(username);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("User {Username} not found in Keycloak", username);
            return false;
        }        

        var roleMapping = new List<object>
        {
            new { id = roleId, name = "custom_role" }
        };

        var jsonPayload = JsonSerializer.Serialize(roleMapping);
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/realm";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, requestUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to assign role in Keycloak: {Response}", await response.Content.ReadAsStringAsync());
            return false;
        }

        return true;
    }
}

