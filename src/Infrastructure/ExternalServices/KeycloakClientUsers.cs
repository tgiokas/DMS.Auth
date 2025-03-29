using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;

namespace DMS.Auth.Infrastructure.ExternalServices;

public partial class KeycloakClient : IKeycloakClient
{
    public async Task<List<KeycloakUser>> GetUsersAsync()
    {
        var adminToken = await GetAdminAccessTokenAsync();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken?.Access_token);

        var response = await _httpClient.GetAsync($"{_keycloakServerUrl}/admin/realms/{_realm}/users");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch users from Keycloak: {Response}", await response.Content.ReadAsStringAsync());
            return new List<KeycloakUser>();
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();

        var users = JsonSerializer.Deserialize<List<KeycloakUser>>(jsonResponse);

        if (users == null)
        {
            _logger.LogWarning("Empty response body while fetching users from Keycloak.");
            return new List<KeycloakUser>();
        }

        return users;
    }

    public async Task<string?> GetUserIdByUsernameAsync(string username)
    {
        var accessToken = await GetAdminAccessTokenAsync();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken?.Access_token);

        var response = await _httpClient.GetAsync($"{_keycloakServerUrl}/admin/realms/{_realm}/users?username={username}");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch user ID for {Username} from Keycloak", username);
            return null;
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();       

        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            _logger.LogWarning("Empty response body while fetching user from Keycloak.");
            return null;
        }

        try
        {
            //var tokenJson = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
            // return tokenJson.GetProperty("id").GetString();

            //var users = JsonSerializer.Deserialize<List<KeycloakUserDto>>(jsonResponse, new JsonSerializerOptions
            //{
            //    PropertyNameCaseInsensitive = true
            //});

            var users = JsonSerializer.Deserialize<List<KeycloakUser>>(jsonResponse);

            return users?.FirstOrDefault()?.Id;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Keycloak user response: {Content}", jsonResponse);
            return null;
        }
    }

    public async Task<KeycloakCredential?> GetUserCredentialsAsync(string userId)
    {
        var adminToken = await GetAdminAccessTokenAsync();

        var requestUrl = $"{_keycloakServerUrl}admin/realms/{_realm}/users/{userId}/credentials";

        //var requestBody = new Credential { Type = "otp", Temporary = false };

        //var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken?.Access_token);

        var response = await _httpClient.GetAsync(requestUrl);

        var jsonResponse = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {            
            _logger.LogError("Failed to get User Credentials from Keycloak: {Response}", jsonResponse);
            return null;
        }

        var credentials = JsonSerializer.Deserialize<KeycloakCredential>(jsonResponse);

        return credentials;
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

        var jsonPayload = JsonSerializer.Serialize(newUser, new JsonSerializerOptions { WriteIndented = true });

        var accessToken = await GetAdminAccessTokenAsync();

        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken?.Access_token);

        var response = await _httpClient.PostAsync($"{_keycloakServerUrl}/admin/realms/{_realm}/users", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to create user in Keycloak: {Response}", error);
            return false;
        }

        // Keycloak returns 201 Created, with a 'Location' header of the form:
        // http://keycloak-host/admin/realms/<realm>/users/<userId>
        var locationHeader = response.Headers.Location;
        if (locationHeader != null)
        {
            // Extract the ID from the final segment:
            var locationSegments = locationHeader.AbsolutePath.Split('/');
            var createdUserId = locationSegments.LastOrDefault();

            var id = locationSegments.Last();
            
            await SendVerifyEmail(createdUserId);
        }

        return true;
    }

    public async Task<bool> UpdateUserAsync(UserUpdateDto request)
    {
        var userId = await GetUserIdByUsernameAsync(request.Username);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("User {Username} not found in Keycloak", request.Username);
            return false;
        }

        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}";

        var updateData = new
        {
            username = request.NewUsername ?? request.Username,
            email = request.NewEmail ?? request.Email,
            enabled = request.IsEnabled
        };

        var adminToken = await GetAdminAccessTokenAsync();

        var content = new StringContent(JsonSerializer.Serialize(updateData), Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken?.Access_token);

        var response = await _httpClient.PutAsync(requestUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to update user {Username} in Keycloak: {Response}", request.Username, await response.Content.ReadAsStringAsync());
            return false;
        }

        return true;
    }

    public async Task<bool> DeleteUserAsync(string username)
    {
        var userId = await GetUserIdByUsernameAsync(username);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("User {Username} not found in Keycloak", username);
            return false;
        }

        var adminToken = await GetAdminAccessTokenAsync();

        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}";

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken?.Access_token);

        var response = await _httpClient.DeleteAsync(requestUrl);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to delete user {Username} in Keycloak: {Response}", username, await response.Content.ReadAsStringAsync());
            return false;
        }

        return true;
    }  
}
