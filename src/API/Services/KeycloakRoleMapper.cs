using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Authentication.Api.Services;

public class KeycloakRoleMapper
{
    public void MapRolesToClaims(TokenValidatedContext context)
    {
        var identity = context.Principal?.Identity as ClaimsIdentity;
        if (identity == null) return;

        // 1. Realm Roles
        var realmAccessClaim = identity.FindFirst("realm_access");
        if (realmAccessClaim != null)
        {
            try
            {
                using var doc = JsonDocument.Parse(realmAccessClaim.Value);
                if (doc.RootElement.TryGetProperty("roles", out var roles))
                {
                    foreach (var role in roles.EnumerateArray())
                    {
                        string? roleValue = role.GetString()?.ToLowerInvariant();
                        if (!string.IsNullOrEmpty(roleValue))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
                            Console.WriteLine($"Realm role: {roleValue}");
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse realm_access: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("No realm_access claim found.");
        }

        // 2. Client Roles
        var resourceAccessClaim = identity.FindFirst("resource_access");
        if (resourceAccessClaim != null)
        {
            try
            {
                using var doc = JsonDocument.Parse(resourceAccessClaim.Value);

                foreach (var client in doc.RootElement.EnumerateObject())
                {
                    if (client.Value.TryGetProperty("roles", out var clientRoles))
                    {
                        foreach (var role in clientRoles.EnumerateArray())
                        {
                            string? roleValue = role.GetString()?.ToLowerInvariant();
                            if (!string.IsNullOrEmpty(roleValue))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
                                Console.WriteLine($"Client role ({client.Name}): {roleValue}");
                            }
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse resource_access: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("No resource_access claim found.");
        }
    }
}
