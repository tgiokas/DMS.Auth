using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;

namespace DMS.Auth.Infrastructure.ExternalServices;

public class KeycloakClient : IKeycloakClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakClient> _logger;
    private readonly string _keycloakServerUrl;
    private readonly string _realm;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _adminToken;
    private readonly string _redirectUri;

    public KeycloakClient(HttpClient httpClient, IConfiguration configuration, ILogger<KeycloakClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _keycloakServerUrl = _configuration["Keycloak:BaseUrl"];
        _realm = _configuration["Keycloak:Realm"];
        _clientId = _configuration["Keycloak:ClientId"];
        _clientSecret = _configuration["Keycloak:ClientSecret"];
        //_adminToken = _configuration["Keycloak:AdminToken"];
        //_redirectUri = _configuration["Keycloak:AdminToken"];
    }


    // Authenticate Backend Services (Microservices, APIs) using Client Credentials Flow.
    public async Task<TokenDto?> GetServiceAccessTokenAsync()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "client_id", _clientId },
            { "client_secret", _clientSecret },
            { "grant_type", "client_credentials" }
        });

        var response = await _httpClient.PostAsync($"{_keycloakServerUrl}/realms/{_realm}/protocol/openid-connect/token", content);
        if (!response.IsSuccessStatusCode) return null;

        var responseBody = await response.Content.ReadAsStringAsync();
        //return responseBody;
        return JsonSerializer.Deserialize<TokenDto>(responseBody);
    }

    // Authenticate Public Users (Frontend Apps) using Password Grant.    
    public async Task<string?> GetUserAccessTokenAsync(string username, string password)
    {       
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", username),
            new KeyValuePair<string, string>("password", password),
            //new KeyValuePair<string, string>("scope", "openid")
        });

        var response = await _httpClient.PostAsync($"{_keycloakServerUrl}/realms/{_realm}/protocol/openid-connect/token", content);
        if (!response.IsSuccessStatusCode)
            return null;

        var jsonResponse = await response.Content.ReadAsStringAsync();

        var tokenJson = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
        return tokenJson.GetProperty("access_token").GetString();
    }

    // Authenticate Public Users (Frontend Apps) using Authorization Code Flow.
    public async Task<string?> AuthenticateUserWithAuthorizationCodeAsync(string authorizationCode)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret), // Only for Confidential Clients
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", authorizationCode),
            new KeyValuePair<string, string>("redirect_uri", _redirectUri)
        });

        var response = await _httpClient.PostAsync($"{_keycloakServerUrl}/realms/{_realm}/protocol/openid-connect/token", content);
        if (!response.IsSuccessStatusCode)
            return null;

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var tokenJson = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
        return tokenJson.GetProperty("access_token").GetString();
    }

    // Refreshes JWT token.   
    public async Task<TokenDto> RefreshTokenAsync(string refreshToken)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        var response = await _httpClient.PostAsync($"{_keycloakServerUrl}/realms/{_realm}/protocol/openid-connect/token", content);
        if (!response.IsSuccessStatusCode) return null;

        var responseBody = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<TokenDto>(responseBody);
    }


    // Retrieves all users from Keycloak.
    public async Task<List<KeycloakUser>> GetUsersAsync()
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _httpClient.GetAsync($"{_keycloakServerUrl}/admin/realms/{_realm}/users");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch users from Keycloak: {Response}", await response.Content.ReadAsStringAsync());
            return new List<KeycloakUser>();
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<KeycloakUser>>(content);
    }
    
    public async Task<string> GetUserIdByUsernameAsync(string username)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _httpClient.GetAsync($"{_keycloakServerUrl}/admin/realms/{_realm}/users?username={username}");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch user ID for {Username} from Keycloak", username);
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<KeycloakUser>>(content);

        return users?.Count > 0 ? users[0].UserName : null;
    }
    
    public async Task<bool> CreateUserAsync(string username, string email, string password)
    {
        var newUser = new KeycloakUser
        {
            UserName = username,
            Email = email,
            FirstName= "Test2",
            LastName= "User",
            Enabled= true,
            Credentials = new[]
            {
                new Credentials { Type = "password", Value = password, Temporary = false }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(newUser, new JsonSerializerOptions { WriteIndented = true });

        var accessToken = await GetServiceAccessTokenAsync();        

        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.access_token);

        var response = await _httpClient.PostAsync($"{_keycloakServerUrl}/admin/realms/{_realm}/users", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to create user in Keycloak: {Response}", error);
            return false;
        }

        return true;
    }

    public async Task<bool> UpdateUserAsync(UpdateUserDto request)
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
            username = request.NewUsername ?? request.Username, // Keep old username if not changing
            email = request.NewEmail ?? request.Email, // Keep old email if not changing
            enabled = request.IsEnabled
        };

        var content = new StringContent(JsonSerializer.Serialize(updateData), Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _httpClient.PutAsync(requestUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to update user {Username} in Keycloak: {Response}", request.Username, await response.Content.ReadAsStringAsync());
            return false;
        }

        return true;
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

        var content = new StringContent(JsonSerializer.Serialize(newRole), Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _httpClient.PostAsync($"{_keycloakServerUrl}/admin/realms/{_realm}/roles", content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to create role in Keycloak: {Response}", await response.Content.ReadAsStringAsync());
            return false;
        }

        return true;
    }

    public async Task<List<KeycloakRole>> GetUserRolesAsync(string username)
    {
        var userId = await GetUserIdByUsernameAsync(username);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("User {Username} not found in Keycloak", username);
            return new List<KeycloakRole>();
        }

        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/realm";

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _httpClient.GetAsync(requestUrl);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch roles for user {Username} in Keycloak: {Response}", username, await response.Content.ReadAsStringAsync());
            return new List<KeycloakRole>();
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<KeycloakRole>>(content);
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

        var content = new StringContent(JsonSerializer.Serialize(roleMapping), Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _httpClient.PostAsync(requestUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to assign role in Keycloak: {Response}", await response.Content.ReadAsStringAsync());
            return false;
        }

        return true;
    }     

    public async Task<bool> EnableMfaAsync(string userId)
    {
        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}";

        var mfaConfig = new
        {
            requiredActions = new[] { "CONFIGURE_TOTP" }
        };

        var content = new StringContent(JsonSerializer.Serialize(mfaConfig), Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _httpClient.PutAsync(requestUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to enable MFA in Keycloak: {Response}", await response.Content.ReadAsStringAsync());
            return false;
        }

        return true;
    }          
 
    public async Task<bool> LogoutAsync(string refreshToken)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        var response = await _httpClient.PostAsync($"{_keycloakServerUrl}/realms/{_realm}/protocol/openid-connect/logout", content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to log out user: {Response}", await response.Content.ReadAsStringAsync());
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

        var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}";

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _httpClient.DeleteAsync(requestUrl);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to delete user {Username} in Keycloak: {Response}", username, await response.Content.ReadAsStringAsync());
            return false;
        }

        return true;
    }
    
}
