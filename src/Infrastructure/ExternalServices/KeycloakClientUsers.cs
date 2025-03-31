using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;

namespace DMS.Auth.Infrastructure.ExternalServices;

public partial class KeycloakClient : KeycloakApiClient, IKeycloakClient
{
    public async Task<List<KeycloakUser>?> GetUsersAsync()
    {
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUrl);
        
        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<KeycloakUser>>(jsonResponse);
    }

    public async Task<string?> GetUserIdByUsernameAsync(string username)
    {
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users?username={username}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUrl);
        
        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<KeycloakUser>>(jsonResponse);
        return users?.FirstOrDefault()?.Id;
    }

    public async Task<KeycloakCredential?> GetUserCredentialsAsync(string userId)
    {
        var requestUrl = $"{_keycloakServerUrl}admin/realms/{_realm}/users/{userId}/credentials";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUrl);
        
        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<KeycloakCredential>(jsonResponse);
    }

    public async Task<bool> CreateUserAsync(string username, string email, string password)
    {
        var newUser = new KeycloakUser
        {
            UserName = username,
            Email = email,
            Enabled = true,
            EmailVerified = true,
            Credentials = new[]
            {
                new KeycloakCredential { Type = "password", Value = password, Temporary = false }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(newUser);
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, requestUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        var response = await SendRequestAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateUserAsync(UserUpdateDto userUpdateDto)
    {
        var userId = await GetUserIdByUsernameAsync(userUpdateDto.Username);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("User {Username} not found in Keycloak", userUpdateDto.Username);
            return false;
        }

        var updateData = new
        {
            username = userUpdateDto.NewUsername ?? userUpdateDto.Username,
            email = userUpdateDto.NewEmail ?? userUpdateDto.Email,
            enabled = userUpdateDto.IsEnabled
        };

        var jsonPayload = JsonSerializer.Serialize(updateData);
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Put, requestUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        var response = await SendRequestAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteUserAsync(string username)
    {
        var userId = await GetUserIdByUsernameAsync(username);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("User {Username} not found in Keycloak", username);
            return false;
        }

        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Delete, requestUrl);

        var response = await SendRequestAsync(request);
        return response.IsSuccessStatusCode;
    }
}
