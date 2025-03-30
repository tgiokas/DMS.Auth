using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
//using Microsoft.Extensions.Logging;

using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;

namespace DMS.Auth.Infrastructure.ExternalServices;

public partial class KeycloakClient : KeycloakApiClient, IKeycloakClient
{
    public async Task<List<KeycloakUser>> GetUsersAsync()
    {
        var adminToken = await GetAdminAccessTokenAsync();

        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken?.Access_token);

        return await SendRequestAsync<List<KeycloakUser>>(request) ?? new List<KeycloakUser>();
    }

    public async Task<string?> GetUserIdByUsernameAsync(string username)
    {
        var adminToken = await GetAdminAccessTokenAsync();

        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users?username={username}";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken?.Access_token);

        var users = await SendRequestAsync<List<KeycloakUser>>(request);
        return users?.FirstOrDefault()?.Id;
    }

    public async Task<KeycloakCredential?> GetUserCredentialsAsync(string userId)
    {
        var adminToken = await GetAdminAccessTokenAsync();

        var requestUrl = $"{_keycloakServerUrl}admin/realms/{_realm}/users/{userId}/credentials";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken?.Access_token);

        return await SendRequestAsync<KeycloakCredential>(request);
    }

    public async Task<bool> CreateUserAsync(string username, string email, string password)
    {
        var newUser = new KeycloakUser
        {
            UserName = username,
            Email = email,
            Enabled = true,
            EmailVerified = true,
            //RequiredActions = new List<string> { "VERIFY_EMAIL", "CONFIGURE_TOTP" },
            Credentials = new[]
            {
                new KeycloakCredential { Type = "password", Value = password, Temporary = false }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(newUser);

        var adminToken = await GetAdminAccessTokenAsync();

        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, 
            requestUrl, new StringContent(jsonPayload, 
            Encoding.UTF8, "application/json"));


        var response = await SendRequestAsync(request);
        
        if (!response) return false;       

        return true;
    }

    public async Task<bool> UpdateUserAsync(UserUpdateDto request)
    {
        var userId = await GetUserIdByUsernameAsync(request.Username);
        if (string.IsNullOrEmpty(userId))
        {
            //_logger.LogError("User {Username} not found in Keycloak", request.Username);
            return false;
        }

        var updateData = new
        {
            username = request.NewUsername ?? request.Username,
            email = request.NewEmail ?? request.Email,
            enabled = request.IsEnabled
        };

        var jsonPayload = JsonSerializer.Serialize(updateData);
        var accessToken = await GetAdminAccessTokenAsync();

        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}";
        var httpRequest = new HttpRequestMessage(HttpMethod.Put, requestUrl)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken?.Access_token);

        return await SendRequestAsync(httpRequest);
    }

    public async Task<bool> DeleteUserAsync(string username)
    {
        var userId = await GetUserIdByUsernameAsync(username);
        if (string.IsNullOrEmpty(userId))
        {
            //_logger.LogError("User {Username} not found in Keycloak", username);
            return false;
        }

        var adminToken = await GetAdminAccessTokenAsync();

        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}";
        var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken?.Access_token);

        return await SendRequestAsync(request);
    }  
}
