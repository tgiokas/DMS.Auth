using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DMS.Auth.Application.Dtos;

namespace DMS.Auth.Infrastructure.ExternalServices
{
    public abstract class BaseApiClient
    {
        protected readonly HttpClient _httpClient;
        protected readonly IConfiguration _configuration;
        protected readonly ILogger _logger;
        private readonly string _keycloakServerUrl;
        private readonly string _realm;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private string? _adminToken;

        protected BaseApiClient(HttpClient httpClient, IConfiguration configuration, ILogger logger)
        {
            _httpClient = httpClient;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _keycloakServerUrl = _configuration["Keycloak:BaseUrl"] ?? throw new ArgumentNullException("Keycloak:BaseUrl");
            _realm = _configuration["Keycloak:Realm"] ?? throw new ArgumentNullException("Keycloak:Realm");
            _clientId = _configuration["Keycloak:ClientId"] ?? throw new ArgumentNullException("Keycloak:ClientId");
            _clientSecret = _configuration["Keycloak:ClientSecret"] ?? throw new ArgumentNullException("Keycloak:ClientSecret");
        }

        protected async Task<string?> GetAdminTokenAsync()
        {
            if (_adminToken == null)
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret),
                });

                var response = await _httpClient.PostAsync($"{_keycloakServerUrl}/realms/{_realm}/protocol/openid-connect/token", content);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get admin token: {Response}", await response.Content.ReadAsStringAsync());
                    return null;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var tokenDto = JsonSerializer.Deserialize<TokenDto>(jsonResponse);
                _adminToken = tokenDto?.Access_token;
            }

            return _adminToken;
        }

        protected async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url, HttpContent? content = null)
        {
            var token = await GetAdminTokenAsync();
            var request = new HttpRequestMessage(method, url)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }
    }
}
