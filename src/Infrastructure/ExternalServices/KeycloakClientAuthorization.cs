using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Authentication.Application.Interfaces;
using Authentication.Infrastructure.ApiClients;

namespace Authentication.Infrastructure.ExternalServices;

public class KeycloakClientAuthorization : KeycloakApiClient, IKeycloakClientAuthorization
{   
    public KeycloakClientAuthorization(HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<KeycloakClientAuthorization> logger, 
        IDistributedCache cache)
       : base(httpClient, configuration, logger, cache)
    {
    }

    public async Task<bool> IsAuthorizedAsync(string accessToken, string resource, string scope)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:uma-ticket"),
            new KeyValuePair<string, string>("audience", _clientId),
            new KeyValuePair<string, string>("permission", $"{resource}#{scope}")
        });

        var requestUrl = $"{_authority}/protocol/openid-connect/token";

        var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, requestUrl);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }
}
