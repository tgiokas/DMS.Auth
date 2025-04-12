using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Authentication.Application.Dtos;
using Authentication.Infrastructure.ApiClient;

namespace Authentication.Infrastructure.ExternalServices;

public abstract class KeycloakApiClient : ApiClientBase
{
    protected readonly IConfiguration _configuration;
    protected readonly string _keycloakServerUrl;
    protected readonly string _realm;
    protected readonly string _clientId;
    protected readonly string _clientSecret;
    protected readonly string _redirectUri;

    protected KeycloakApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<KeycloakClient> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _keycloakServerUrl = _configuration["Keycloak:BaseUrl"] ?? throw new ArgumentNullException("Keycloak:BaseUrl");
        _realm = _configuration["Keycloak:Realm"] ?? throw new ArgumentNullException("Keycloak:Realm");
        _clientId = _configuration["Keycloak:ClientId"] ?? throw new ArgumentNullException("Keycloak:ClientId");
        _clientSecret = _configuration["Keycloak:ClientSecret"] ?? throw new ArgumentNullException("Keycloak:ClientSecret");
        _redirectUri = _configuration["Keycloak:RedirectUrl"] ?? "";
    }

    // Authenticate Backend Services (Microservices, APIs) using Client Credentials Flow (Return a JWT Admin Token).
    protected async Task<TokenDto?> GetAdminAccessTokenAsync()
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
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

    protected async Task<HttpRequestMessage> CreateAuthenticatedRequestAsync(HttpMethod method, string requestUrl, HttpContent? content = null)
    {
        var adminToken = await GetAdminAccessTokenAsync();
        var request = new HttpRequestMessage(method, requestUrl)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken?.Access_token);
        return request;
    }
}