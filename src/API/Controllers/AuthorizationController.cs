using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Authentication.Application.Interfaces;

namespace Authentication.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthorizationController : ControllerBase
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<AuthorizationController> _logger;

    public AuthorizationController(
        IAuthorizationService authorizationService,
        ILogger<AuthorizationController> logger)
    {
        _authorizationService = authorizationService;
        _logger = logger;
    }

    [HttpGet, HttpPost]
    public async Task<IActionResult> Authorize()
    {
        var token = ExtractBearerToken(Request, out var errorResult);
        if (token is null)
            return errorResult!;

        var path = Request.Headers["X-Forwarded-Uri"].FirstOrDefault() ?? "/";
        var method = Request.Headers["X-Forwarded-Method"].FirstOrDefault() ?? "GET";

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        try
        {
            var resourceAccess = jwt.Payload["resource_access"] as JsonElement?;
            if (resourceAccess == null || !resourceAccess.Value.TryGetProperty("auth-app", out var authApp))
            {
                _logger.LogWarning("Missing resource_access.auth-app in token");
                return Forbid("Missing auth-app role data");
            }

            if (!authApp.TryGetProperty("roles", out var rolesElement) ||
                rolesElement.ValueKind != JsonValueKind.Array ||
                !rolesElement.EnumerateArray().Any())
            {
                _logger.LogWarning("Missing roles array in resource_access.auth-app");
                return Forbid("Missing roles array");
            }

            // Take only the first role
            var firstRole = rolesElement.EnumerateArray().First().GetString();
            if (string.IsNullOrWhiteSpace(firstRole))
            {
                _logger.LogWarning("Empty role in resource_access.auth-app");
                return Forbid("Invalid role data");
            }

            var isAuthorized = await _authorizationService.IsAuthorizedAsync(firstRole, path, method);
            return isAuthorized ? Ok() : Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing token");
            return Forbid("Invalid token structure");
        }
    }

    private static string? ExtractBearerToken(HttpRequest req, out IActionResult? error)
    {
        error = null;
        var auth = req.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer "))
        {
            error = new UnauthorizedObjectResult("Missing Bearer token");
            return null;
        }

        return auth["Bearer ".Length..].Trim();
    }
}
