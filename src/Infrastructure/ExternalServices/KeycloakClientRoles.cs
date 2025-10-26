using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;
using Authentication.Infrastructure.ApiClients;

namespace Authentication.Infrastructure.ExternalServices;

public class KeycloakClientRole : KeycloakApiClient, IKeycloakClientRole
{
    public KeycloakClientRole(HttpClient httpClient,
        IConfiguration configuration,
        ILogger<KeycloakClientRole> logger,
        IDistributedCache cache)
    : base(httpClient, configuration, logger, cache)
    {
    }

    public async Task<List<KeycloakRole>?> GetRolesAsync()
    {
        await GetClientUuidAsync();
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/clients/{_clientUuid}/roles";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUrl);

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<KeycloakRole>>(jsonResponse);
    }

    public async Task<KeycloakRole?> GetRoleByNameAsync(string roleName)
    {
        await GetClientUuidAsync();
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/clients/{_clientUuid}/roles/{roleName}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUrl);

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<KeycloakRole>(jsonResponse);
    }

    public async Task<KeycloakRole?> GetRoleByIdAsync(string roleId)
    {
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/roles-by-id/{roleId}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUrl);

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<KeycloakRole>(jsonResponse);
    }

    public async Task<List<KeycloakRole>?> GetUserRolesAsync(string userId)
    {
        await GetClientUuidAsync();
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/clients/{_clientUuid}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUrl);

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch client roles for user {UserId} in Keycloak: {Response}", userId, await response.Content.ReadAsStringAsync());
            return null;
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<KeycloakRole>>(jsonResponse);
    }

    public async Task<KeycloakRole?> CreateRoleAsync(string roleName, string roleDescr)
    {
        await GetClientUuidAsync();
        var newRole = new KeycloakRole
        {
            Name = roleName,
            Description = roleDescr,
            Composite = false,
            ClientRole = true,
            ContainerId = _clientUuid
        };

        var jsonPayload = JsonSerializer.Serialize(newRole);
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/clients/{_clientUuid}/roles";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, requestUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to create client role: {Response}", await response.Content.ReadAsStringAsync());
            return null;
        }

        return newRole;
    }

    public async Task<KeycloakRole?>UpdateRoleAsync(RoleUpdateDto roleDto)
    {
        await GetClientUuidAsync();
        var newRole = new KeycloakRole
        {
            Name = roleDto.NewRoleName?? roleDto.RoleName,
            Description = roleDto.Description,
            Composite = false,
            ClientRole = true,
            ContainerId = _clientUuid
        };

        var jsonPayload = JsonSerializer.Serialize(newRole);
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/clients/{_clientUuid}/roles/{roleDto.RoleName}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Put, requestUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to update client role: {Response}", await response.Content.ReadAsStringAsync());
            return null;
        }

        return newRole;
    }

    public async Task<bool> DeleteRoleAsync(string roleName)
    {
        await GetClientUuidAsync();
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/clients/{_clientUuid}/roles/{roleName}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Delete, requestUrl);

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Failed to delete client role {roleName}: {error}");
            return false;
        }

        return true;
    }

    public async Task<bool> AssignRoleAsync(string userId, string roleId, string roleName)
    {
        await GetClientUuidAsync();
        var roleMapping = new List<object> { new { id = roleId, name = roleName } };
        var jsonPayload = JsonSerializer.Serialize(roleMapping);
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/clients/{_clientUuid}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, requestUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Failed to assign client role:  {roleName}: {error}");
            return false;
        }

        return true;
    }

    public async Task<bool> RemoveRoleAsync(string userId, string roleId, string roleName)
    {
        await GetClientUuidAsync();
        var roleMapping = new List<object>
        {
            new { id = roleId, name = roleName }
        };

        var jsonPayload = JsonSerializer.Serialize(roleMapping);
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/clients/{_clientUuid}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Delete, requestUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to remove client role from user in Keycloak: {Response}", await response.Content.ReadAsStringAsync());
            return false;
        }

        return true;
    }
}
