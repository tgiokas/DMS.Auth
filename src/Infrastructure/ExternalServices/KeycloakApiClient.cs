using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using DMS.Auth.Application.Dtos;

namespace DMS.Auth.Infrastructure.ExternalServices
{
    public abstract class KeycloakApiClient
    {
        protected readonly HttpClient _httpClient;
        protected readonly IConfiguration _configuration;
        protected readonly ILogger<KeycloakClient> _logger;
        protected readonly string _keycloakServerUrl;
        protected readonly string _realm;
        protected readonly string _clientId;
        protected readonly string _clientSecret;
        protected readonly string _redirectUri;

        const string LogMessageTemplate =
            "HTTP {Direction} {RequestMethod} {RequestPath} {RequestPayload} responded {HttpStatusCode} {ResponsePayload} in {Elapsed:0.0000} ms";

        const string ErrorMessageTemplate =
            "ERROR {Direction} {RequestMethod} {RequestPath} {RequestPayload} responded {HttpStatusCode} {ResponsePayload}";

        protected KeycloakApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<KeycloakClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

            return await SendRequestAsync<TokenDto>(request);
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

        protected async Task<T?> SendRequestAsync<T>(HttpRequestMessage request)
        {
            string requestBody = request.Content != null ? await request.Content.ReadAsStringAsync() : string.Empty;
            var sw = Stopwatch.StartNew();

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ErrorMessageTemplate, "Outgoing", request.Method,
                    request.RequestUri, requestBody, HttpStatusCode.ServiceUnavailable, "");
                throw;
            }

            sw.Stop();
            string responseBody = await response.Content.ReadAsStringAsync();
            int statusCode = (int)response.StatusCode;
            LogLevel logLevel = statusCode > 499 ? LogLevel.Error : LogLevel.Information;

            _logger.Log(logLevel, LogMessageTemplate, "Outgoing", request.Method, 
                    request.RequestUri, requestBody, statusCode, responseBody, (long)sw.ElapsedMilliseconds);

            return JsonSerializer.Deserialize<T>(responseBody);
        }

        protected async Task<bool> SendRequestAsync(HttpRequestMessage request)
        {
            string requestBody = request.Content != null ? await request.Content.ReadAsStringAsync() : string.Empty;
            var sw = Stopwatch.StartNew();

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ErrorMessageTemplate, "Outgoing", request.Method,
                    request.RequestUri, requestBody, HttpStatusCode.ServiceUnavailable, "");
                throw;
            }

            sw.Stop();
            string responseBody = await response.Content.ReadAsStringAsync();
            int statusCode = (int)response.StatusCode;
            LogLevel logLevel = statusCode > 499 ? LogLevel.Error : LogLevel.Information;

            _logger.Log(logLevel, LogMessageTemplate, "Outgoing", request.Method,
                    request.RequestUri, requestBody, statusCode, responseBody, (long)sw.ElapsedMilliseconds);

            return response.IsSuccessStatusCode;
        }
    }
}
