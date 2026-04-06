using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Authentication.Application.Configuration;
using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;
using Authentication.Infrastructure.ApiClients;

namespace Authentication.Infrastructure.ExternalServices;

public class KeycloakClientAuthentication : KeycloakApiClient, IKeycloakClientAuthentication
{
    public KeycloakClientAuthentication(HttpClient httpClient,
        IOptions<KeycloakSettings> keycloakOptions,
        ILogger<KeycloakClientAuthentication> logger,
        IDistributedCache cache)
    : base(httpClient, keycloakOptions, logger, cache)
    {
    }

    // Get Access Token using password (Direct Access Grant).
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

    // Get Access Token using authorization code (Authorization Code Grant).
    public async Task<TokenDto?> GetAccessTokenByCodeAsync(string code)
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
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Refresh token failed with {StatusCode}: {Body}", (int)response.StatusCode, errorBody);
            return null;
        }

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

    public Task<string> EntraLoginUrlAsync()
    {
        string idpProvider = "cbs-entra";

        string loginUrl = $"{_keycloakServerUrl}/realms/{_realm}/protocol/openid-connect/auth" +
             $"?client_id={Uri.EscapeDataString(_clientId)}" +
             $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}" +
             $"&response_type=code" +
             $"&scope={Uri.EscapeDataString("openid profile email")}" +
             $"&kc_idp_hint={Uri.EscapeDataString(idpProvider)}";

        return Task.FromResult(loginUrl);
    }
}
