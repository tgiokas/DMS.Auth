using System.Net.Http;
using System.Text;
using System.Text.Json;
using DMS.Auth.Domain.Interfaces;

namespace DMS.Auth.Infrastructure.Keycloak
{
    public class KeycloakClient : IKeycloakClient
    {
        private readonly HttpClient _httpClient;

        // Possibly store Keycloak base URL, credentials, or use a separate service
        // to fetch them from a config table or secrets store.
        public KeycloakClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> CreateUserAsync(string realm, string username, string email)
        {
            // 1. Get admin token
            var accessToken = await GetAdminAccessTokenAsync(realm);

            // 2. Build request body
            var userPayload = new
            {
                username = username,
                email = email,
                enabled = true
            };
            var jsonPayload = JsonSerializer.Serialize(userPayload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // 3. POST to Keycloak Admin API
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_httpClient.BaseAddress}/admin/realms/{realm}/users")
            {
                Content = content
            };

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // Keycloak returns the location header with user ID
            // e.g. Location: http://<base>/admin/realms/{realm}/users/{keycloakUserId}
            var locationHeader = response.Headers.Location;
            var keycloakUserId = locationHeader?.Segments.LastOrDefault() ?? Guid.NewGuid().ToString();

            return keycloakUserId;
        }

        public async Task EnableMfaAsync(string realm, string keycloakUserId)
        {
            // Implementation depends on how you configure MFA in Keycloak.
            // Typically, you'd update the user's required actions or 
            // set up the TOTP authenticator.

            var accessToken = await GetAdminAccessTokenAsync(realm);

            var requiredActions = new
            {
                requiredActions = new string[] { "CONFIGURE_TOTP" }
            };

            var jsonPayload = JsonSerializer.Serialize(requiredActions);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"{_httpClient.BaseAddress}/admin/realms/{realm}/users/{keycloakUserId}")
            {
                Content = content
            };

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateUserEmailAsync(string realm, string keycloakUserId, string newEmail)
        {
            var token = await GetAdminAccessTokenAsync(realm);

            var updatePayload = new
            {
                email = newEmail
            };

            var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"{_httpClient.BaseAddress}/admin/realms/{realm}/users/{keycloakUserId}")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(updatePayload),
                    Encoding.UTF8,
                    "application/json")
            };

            request.Headers.Authorization
                = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteUserAsync(string realm, string keycloakUserId)
        {
            var token = await GetAdminAccessTokenAsync(realm);

            var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"{_httpClient.BaseAddress}/admin/realms/{realm}/users/{keycloakUserId}");

            request.Headers.Authorization
                = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        public async Task AssignRoleAsync(string realm, string keycloakUserId, string roleName)
        {
            var token = await GetAdminAccessTokenAsync(realm);

            // 1. You must look up the role ID by name.
            //    Keycloak Admin API: GET /admin/realms/{realm}/roles/{roleName}
            var roleLookupRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_httpClient.BaseAddress}/admin/realms/{realm}/roles/{roleName}");
            roleLookupRequest.Headers.Authorization
                = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var lookupResponse = await _httpClient.SendAsync(roleLookupRequest);
            lookupResponse.EnsureSuccessStatusCode();

            var roleJson = await lookupResponse.Content.ReadAsStringAsync();
            var roleObj = JsonSerializer.Deserialize<RoleRepresentation>(roleJson);
            // roleObj should contain info like { id, name, composite, clientRole }

            // 2. Assign role to user:
            // POST /admin/realms/{realm}/users/{keycloakUserId}/role-mappings/realm
            // Body: array of roles
            var rolesPayload = new[]
            {
                new {
                    id = roleObj.id,
                    name = roleObj.name
                }
            };
            var rolesRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_httpClient.BaseAddress}/admin/realms/{realm}/users/{keycloakUserId}/role-mappings/realm")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(rolesPayload),
                    Encoding.UTF8,
                    "application/json")
            };
            rolesRequest.Headers.Authorization
                = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var assignResponse = await _httpClient.SendAsync(rolesRequest);
            assignResponse.EnsureSuccessStatusCode();
        }   

        private class RoleRepresentation
        {
            public string id { get; set; }
            public string name { get; set; }
            public bool clientRole { get; set; }
            public bool composite { get; set; }
            // etc. (Keycloak returns more fields, but you only need what you plan to use)
        }    

        private async Task<string> GetAdminAccessTokenAsync(string realm)
        {
            // In a real scenario, you might store the admin credentials or
            // client credentials to retrieve an admin token for the realm.
            // This is a simplified approach using "client_credentials".

            var tokenRequest = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", "admin-cli"),
                new KeyValuePair<string, string>("client_secret", "your-keycloak-secret")
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_httpClient.BaseAddress}/realms/{realm}/protocol/openid-connect/token")
            {
                Content = new FormUrlEncodedContent(tokenRequest)
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            return tokenResponse["access_token"].ToString();
        }
    }
}
