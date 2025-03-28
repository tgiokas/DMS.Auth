using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;

namespace DMS.Auth.Infrastructure.ExternalServices;

public partial class KeycloakClient : IKeycloakClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakClient> _logger;
    private readonly string _keycloakServerUrl;
    private readonly string _realm;
    private readonly string _clientId;
    private readonly string _clientSecret;    
    private readonly string _redirectUri;

    public KeycloakClient(HttpClient httpClient, IConfiguration configuration, ILogger<KeycloakClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _keycloakServerUrl = _configuration["Keycloak:BaseUrl"] ?? throw new ArgumentNullException("Keycloak:BaseUrl");
        _realm = _configuration["Keycloak:Realm"] ?? throw new ArgumentNullException("Keycloak:Realm");
        _clientId = _configuration["Keycloak:ClientId"] ?? throw new ArgumentNullException("Keycloak:ClientId");
        _clientSecret = _configuration["Keycloak:ClientSecret"] ?? throw new ArgumentNullException("Keycloak:ClientSecret");
        _redirectUri = _configuration["Keycloak:RedirectUrl"] ?? throw new ArgumentNullException("Keycloak:RedirectUrl");
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
                    //new KeyValuePair<string, string>("scope", "openid")
                });

        var response = await _httpClient.PostAsync($"{_keycloakServerUrl}/realms/{_realm}/protocol/openid-connect/token", content);
        if (!response.IsSuccessStatusCode)
            return null;

        var jsonResponse = await response.Content.ReadAsStringAsync();

        //var tokenJson = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
        //return tokenJson.GetProperty("access_token").GetString();

        return JsonSerializer.Deserialize<TokenDto>(jsonResponse);
    }

    // Authenticate Backend Services (Microservices, APIs) using Client Credentials Flow (Return a JWT Admin Token).
    public async Task<TokenDto?> GetAdminAccessTokenAsync()
    {
        var content = new FormUrlEncodedContent(new[]
        {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret),
                });

        var response = await _httpClient.PostAsync($"{_keycloakServerUrl}/realms/{_realm}/protocol/openid-connect/token", content);
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

        var response = await _httpClient.PostAsync($"{_keycloakServerUrl}/realms/{_realm}/protocol/openid-connect/token", content);
        if (!response.IsSuccessStatusCode) return null;

        var responseBody = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<TokenDto>(responseBody);
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
        

    private async Task<bool> SendVerifyEmail(string? userId, string? adminToken = null)
    {
        if (adminToken == null)
        {
            var accessToken = await GetAdminAccessTokenAsync();
            adminToken = accessToken?.Access_token;
        }
        var requestUrl = $"{_keycloakServerUrl}admin/realms/{_realm}/users/{userId}/send-verify-email";

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.PutAsync(requestUrl, null);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to send email MFA in Keycloak: {Response}", await response.Content.ReadAsStringAsync());
            return false;
        }

        return true;
    }

    private async Task<bool> ExecuteActionsEmail(string? userId, string? adminToken = null)
    {
        if (adminToken == null)
        {
            var accessToken = await GetAdminAccessTokenAsync();
            adminToken = accessToken?.Access_token;
        }
        var requestUrl = $"{_keycloakServerUrl}admin/realms/{_realm}/users/{userId}/execute-actions-email";

        //var requestBody0 = new
        //{
        //    requiredActions = new[] { "CONFIGURE_TOTP" }
        //};

        var requestBody = new[] { "CONFIGURE_TOTP" };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.PutAsync(requestUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to send email MFA in Keycloak: {Response}", await response.Content.ReadAsStringAsync());
            return false;
        }

        return true;
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

        var response = await _httpClient.PostAsync($"{_keycloakServerUrl}/realms/{_realm}/protocol/openid-connect/token", content);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenDto>();
        return tokenResponse;
    }

    //public async Task<bool> EnableMfaAsync(string userId)
    //{
    //    var requestUrl = $"{_keycloakServerUrl}/admin/realms/{_realm}/users/{userId}";

    //    var mfaConfig = new
    //    {
    //        requiredActions = new[] { "CONFIGURE_TOTP" }
    //    };

    //    var content = new StringContent(JsonSerializer.Serialize(mfaConfig), Encoding.UTF8, "application/json");

    //    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

    //    var response = await _httpClient.PutAsync(requestUrl, content);

    //    if (!response.IsSuccessStatusCode)
    //    {
    //        _logger.LogError("Failed to enable MFA in Keycloak: {Response}", await response.Content.ReadAsStringAsync());
    //        return false;
    //    }

    //    return true;
    //}

    // Authenticate Public Users (Frontend Apps) using Authorization Code Flow.
    //public async Task<string?> AuthenticateUserWithAuthorizationCodeAsync(string authorizationCode)
    //{
    //    var content = new FormUrlEncodedContent(new[]
    //    {
    //        new KeyValuePair<string, string>("grant_type", "authorization_code"),
    //        new KeyValuePair<string, string>("client_id", _clientId),
    //        new KeyValuePair<string, string>("client_secret", _clientSecret),
    //        new KeyValuePair<string, string>("code", authorizationCode),
    //        new KeyValuePair<string, string>("redirect_uri", _redirectUri)
    //    });

    //    var response = await _httpClient.PostAsync($"{_keycloakServerUrl}/realms/{_realm}/protocol/openid-connect/token", content);
    //    if (!response.IsSuccessStatusCode)
    //        return null;

    //    //if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
    //    //{
    //    //    var authSessionCookie = cookies.FirstOrDefault(c => c.StartsWith("AUTH_SESSION_ID="));

    //    //    if (!string.IsNullOrEmpty(authSessionCookie))
    //    //    {
    //    //        var sessionCode = authSessionCookie.Split(';')[0].Replace("AUTH_SESSION_ID=", "");

    //    //        return new TokenTempDto { Code = sessionCode };

    //    //        ////return JsonSerializer.Deserialize<TokenTempDto>(sessionCode);
    //    //    }
    //    //}  

    //    var jsonResponse = await response.Content.ReadAsStringAsync();
    //    var tokenJson = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
    //    return tokenJson.GetProperty("access_token").GetString();
    //}
}
