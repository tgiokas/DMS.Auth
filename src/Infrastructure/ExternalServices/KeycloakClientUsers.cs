﻿using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;

namespace Authentication.Infrastructure.ExternalServices;

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
        var user = await GetUserProfileAsync(username);
        return user?.Id;
    }

    public async Task<KeycloakUser?> GetUserProfileAsync(string username)
    {
        var requestUrl = $"{_keycloakServerUrl}admin/realms/{_realm}/users?username={username}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUrl);
        
        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<KeycloakUser>>(jsonResponse);
        return users?.FirstOrDefault();
    }

    public async Task<KeycloakUser?> GetUserByEmailAsync(string email)
    {
        var requestUrl = $"{_keycloakServerUrl}admin/realms/{_realm}/users?exact=true?email={email}";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUrl);

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<KeycloakUser>>(jsonResponse);
        return users?.FirstOrDefault();
    }

    public async Task<string> CreateUserAsync(string username, string email, string password)
    {
        bool emailVerified = bool.Parse(_configuration["EmailVerified"] ?? "true");
        var createdUserId = string.Empty;

        var newUser = new KeycloakUser
        {
            UserName = username,
            Email = email,
            Enabled = true,
            EmailVerified = emailVerified,  
            Credentials = new[]
            {
                new KeycloakCredential { Type = "password", Value = password, Temporary = false }
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
            return createdUserId;
        }

        // Keycloak returns 201 Created, with a 'Location' header of the form:
        // http://keycloak-host/admin/realms/<realm>/users/<userId>
        var locationHeader = response.Headers.Location;

        if (locationHeader != null)
        {
            // Extract the ID from the final segment:
            var locationSegments = locationHeader.AbsolutePath.Split('/');
            createdUserId = locationSegments.LastOrDefault() ?? string.Empty;                  
        }

        return createdUserId;
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

    public async Task UpdateUserPasswordAsync(string userId, string newPassword)
    {
        var resetPayload = new
        {
            type = "password",
            value = newPassword,
            temporary = false
        };

        var jsonPayload = JsonSerializer.Serialize(resetPayload);
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}/reset-password";
        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Put, requestUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        var response = await SendRequestAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to reset password for user {UserId}: {Error}", userId, error);
            throw new ApplicationException("Failed to reset password");
        }
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
