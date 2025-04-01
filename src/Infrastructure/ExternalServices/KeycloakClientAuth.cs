using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;

namespace DMS.Auth.Infrastructure.ExternalServices;

public partial class KeycloakClient : KeycloakApiClient, IKeycloakClient
{
    public KeycloakClient(HttpClient httpClient, IConfiguration configuration, ILogger<KeycloakClient> logger)
        : base(httpClient, configuration, logger)
    {
    }

    // Authenticate Public Users (Frontend Apps) using Password Grant (Return a JWT token).
    public async Task<TokenDto?> GetUserAccessTokenAsync(string username, string password)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("username", username),
            new KeyValuePair<string, string>("password", password),
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakServerUrl}/realms/{_realm}/protocol/openid-connect/token")
        {
            Content = content
        };

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TokenDto>(jsonResponse);
    }

    public async Task<TokenDto?> RefreshTokenAsync(string refreshToken)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakServerUrl}/realms/{_realm}/protocol/openid-connect/token")
        {
            Content = content
        };

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TokenDto>(jsonResponse);
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakServerUrl}/realms/{_realm}/protocol/openid-connect/logout")
        {
            Content = content
        };

        var response = await SendRequestAsync(request);
        return response.IsSuccessStatusCode;
    }

    private async Task<bool> SendVerificationEmail(string? userId, string? adminToken = null)
    {
        if (adminToken == null)
        {
            var accessToken = await GetAdminAccessTokenAsync();
            adminToken = accessToken?.Access_token;
        }
        var requestUrl = $"{_keycloakServerUrl}admin/realms/{_realm}/users/{userId}/send-verify-email";

        var request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await SendRequestAsync(request);
        return response.IsSuccessStatusCode;
    }

    private async Task<bool> ExecuteActionsEmail(string? userId, string? adminToken = null)
    {
        if (adminToken == null)
        {
            var accessToken = await GetAdminAccessTokenAsync();
            adminToken = accessToken?.Access_token;
        }
        var requestUrl = $"{_keycloakServerUrl}admin/realms/{_realm}/users/{userId}/execute-actions-email";

        var requestBody = new[] { "CONFIGURE_TOTP" };
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Put, requestUrl)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await SendRequestAsync(request);
        return response.IsSuccessStatusCode;
    }

    public Task<string> GsisLoginUrl()
    {
        string gsisProvider = "gsis"; // Identity Provider alias in Keycloak

        string loginUrl = $"{_keycloakServerUrl}/realms/{_realm}/protocol/openid-connect/auth" +
            $"?client_id={_clientId}" +
            $"&redirect_uri={_redirectUri}" +
            $"&response_type=code" +
            $"&kc_idp_hint={gsisProvider}";

        return Task.FromResult(loginUrl);
    }

    public async Task<TokenDto?> GsisCallback(string code)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", _redirectUri)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakServerUrl}/realms/{_realm}/protocol/openid-connect/token")
        {
            Content = content
        };

        var response = await SendRequestAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenDto>();
        return tokenResponse;
    }
}
