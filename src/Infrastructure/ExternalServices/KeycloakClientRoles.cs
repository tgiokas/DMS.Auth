using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;

namespace DMS.Auth.Infrastructure.ExternalServices;

public partial class KeycloakClient : IKeycloakClient
{     
    public async Task<List<KeycloakRole>?> GetUserRolesAsync(string username)
    {
        var userId = await GetUserIdByUsernameAsync(username);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("User {Username} not found in Keycloak", username);
            return null;
        }

        var adminToken = await GetAdminAccessTokenAsync();       

        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/realm";

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken?.Access_token);

        var response = await _httpClient.GetAsync(requestUrl);

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

        var adminToken = await GetAdminAccessTokenAsync();

        var content = new StringContent(JsonSerializer.Serialize(newRole), Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken?.Access_token);

        var response = await _httpClient.PostAsync($"{_keycloakServerUrl}/admin/realms/{_realm}/roles", content);

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

        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/realm";

        var roleMapping = new List<object>
        {
            new { id = roleId, name = "custom_role" }
        };

        var adminToken = await GetAdminAccessTokenAsync();

        var content = new StringContent(JsonSerializer.Serialize(roleMapping), Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken?.Access_token);

        var response = await _httpClient.PostAsync(requestUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to assign role in Keycloak: {Response}", await response.Content.ReadAsStringAsync());
            return false;
        }

        return true;
    }     
}
