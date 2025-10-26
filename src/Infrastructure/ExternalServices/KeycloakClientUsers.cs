using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;
using Authentication.Infrastructure.ApiClients;

namespace Authentication.Infrastructure.ExternalServices;

public class KeycloakClientUser : KeycloakApiClient, IKeycloakClientUser
{
    public KeycloakClientUser(HttpClient httpClient,
        IConfiguration configuration,
        ILogger<KeycloakClientUser> logger,
        IDistributedCache cache)
    : base(httpClient, configuration, logger, cache)
    {
    }

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
        var user = await GetUserByNameAsync(username);
        return user?.Id;
    }

    public async Task<KeycloakUser?> GetUserByNameAsync(string username)
    {
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users?exact=true&username={username}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUrl);
        
        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<KeycloakUser>>(jsonResponse);
        return users?.FirstOrDefault();
    }

    public async Task<KeycloakUser?> GetUserByIdAsync(string userId)
    {
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUrl);

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<KeycloakUser>(jsonResponse);
        return user;
    }

    public async Task<KeycloakUser?> GetUserByEmailAsync(string email)
    {
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users?exact=true&email={email}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUrl);

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<KeycloakUser>>(jsonResponse);
        return users?.FirstOrDefault();
    }

    public async Task<KeycloakUser?> CreateUserAsync(KeycloakUserDto userCreateDto)
    {
        var newUser = new KeycloakUser
        {
            UserName = userCreateDto.Username,
            Email = userCreateDto.Email,
            FirstName = userCreateDto.FirstName,
            LastName = userCreateDto.LastName,
            Enabled = userCreateDto.Enabled,
            EmailVerified = userCreateDto.EmailVerified,
            Credentials = new[]
            {
                new KeycloakCredential { Type = "password", Value = userCreateDto.Password, Temporary = userCreateDto.PasswordTemp }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(newUser);
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, requestUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        var response = await SendRequestAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to create user in Keycloak: {Response}", error);
            return null;
        }

        // Keycloak returns 201 Created, with a 'Location' header of the form:
        // http://keycloak-host/admin/realms/<realm>/users/<userId>
        var locationHeader = response.Headers.Location;

        if (locationHeader != null)
        {
            // Extract the ID from the final segment:
            var locationSegments = locationHeader.AbsolutePath.Split('/');
            string? createdUserId = locationSegments.LastOrDefault() ?? string.Empty;
            newUser.Id = createdUserId;

            return newUser;
        }

        return null;
    }

    public async Task<Result<bool>> UpdateUserAsync(KeycloakUserDto userUpdateDto)
    {
        var updateData = new Dictionary<string, object?>
        {
            ["firstName"] = userUpdateDto.FirstName,
            ["lastName"] = userUpdateDto.LastName,
            ["enabled"] = userUpdateDto.Enabled,
            ["email"] = userUpdateDto.Email,
            ["emailVerified"] = userUpdateDto.EmailVerified,
        };

        // Update username if provided
        if (!string.IsNullOrWhiteSpace(userUpdateDto.Username))
        {
            updateData["username"] = userUpdateDto.Username;
        }

        var jsonPayload = JsonSerializer.Serialize(updateData);
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userUpdateDto.Id}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Put, requestUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        var response = await SendRequestAsync(request);

        if (response.IsSuccessStatusCode)
        {          
            return Result<bool>.Ok(true, "User updated successfully in keycloak.");
        }
        else if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadAsStringAsync();
            var errorMsg = $"Conflict: {error}";
            _logger.LogError(errorMsg);
            return Result<bool>.Fail(errorMsg, "AUTH-048");
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            var errorMsg = $"Failed to update user {userUpdateDto.Id} in Keycloak: {error}";
            _logger.LogError(errorMsg);
            return Result<bool>.Fail(errorMsg, "AUTH-011");
        }
    }

    public async Task<bool> UpdateUserPasswordAsync(string userId, string newPassword, bool temporary)
    {
        var resetPayload = new
        {
            type = "password",
            value = newPassword,
            temporary = temporary
        };

        var jsonPayload = JsonSerializer.Serialize(resetPayload);
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}/reset-password";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Put, requestUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        var response = await SendRequestAsync(request);        

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to reset password for user {UserId}: {Error}", userId, error);            
        }

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {     
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Delete, requestUrl);

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            var errorMsg = $"Failed to delete user with ID: {userId}) in Keycloak: {error}";
            _logger.LogError(errorMsg);
            return false;
        }

        return true;
    }

    public async Task<IDictionary<string, string[]>> GetUserAttributesAsync(string userId)
    {
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUrl);

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
            return new Dictionary<string, string[]>();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonResponse);
        if (doc.RootElement.TryGetProperty("attributes", out var attributesElement))
        {
            var attributes = JsonSerializer.Deserialize<Dictionary<string, string[]>>(attributesElement.GetRawText());
            return attributes ?? new Dictionary<string, string[]>();
        }
        return new Dictionary<string, string[]>();
    }

    public async Task<bool> SetUserAttributeAsync(string userId, string key, string value)
    {
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUrl);
        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
            return false;

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse);

        if (user is null)
            return false;

        // Extract attributes
        Dictionary<string, string[]> attributes;
        if (user.TryGetValue("attributes", out var attributesObj)
            && attributesObj is JsonElement attributesElement
            && attributesElement.ValueKind == JsonValueKind.Object)
        {
            attributes = JsonSerializer.Deserialize<Dictionary<string, string[]>>(attributesElement.GetRawText()) ?? new Dictionary<string, string[]>();
        }
        else
        {
            attributes = new Dictionary<string, string[]>();
        }

        // Update the value
        attributes[key] = new[] { value };
        user["attributes"] = attributes;

        // Send update
        var updatePayload = JsonSerializer.Serialize(user);
        var putRequest = await CreateAuthenticatedRequestAsync(HttpMethod.Put, requestUrl, new StringContent(updatePayload, Encoding.UTF8, "application/json"));
        var putResponse = await SendRequestAsync(putRequest);
        return putResponse.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateUserAttributesAsync(string userId, IDictionary<string, string[]> attributes)
    {
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUrl);
        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
            return false;

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse);

        if (user is null)
            return false;

        user["attributes"] = attributes;

        var updatePayload = JsonSerializer.Serialize(user);
        var putRequest = await CreateAuthenticatedRequestAsync(HttpMethod.Put, requestUrl, new StringContent(updatePayload, Encoding.UTF8, "application/json"));
        var putResponse = await SendRequestAsync(putRequest);
        return putResponse.IsSuccessStatusCode;
    }
}
