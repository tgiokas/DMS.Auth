using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

using Authentication.Application.Dtos;
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
        if (token is null) return errorResult!;

        var path = Request.Headers["X-Forwarded-Uri"].FirstOrDefault() ?? "/";
        var method = Request.Headers["X-Forwarded-Method"].FirstOrDefault() ?? "GET";

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var userId = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "anonymous";

        var deptRolesJson = jwt.Payload["department_roles"]?.ToString();
        if (string.IsNullOrEmpty(deptRolesJson))
        {
            _logger.LogWarning("Missing department_roles in token for user {User}", userId);
            return Forbid("Missing department_roles");
        }

        List<DepartmentRole>? deptRoles;
        try
        {
            deptRoles = JsonSerializer.Deserialize<List<DepartmentRole>>(deptRolesJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize department_roles for user {User}", userId);
            return Forbid("Malformed department_roles claim");
        }
        
        var departmentRolesDict = deptRoles!
            .ToDictionary(
                dr => dr.Department,
                dr => (IReadOnlyList<Guid>)dr.Roles);

        var userContext = new UserContext(userId, departmentRolesDict);

        var ok = await _authorizationService.IsAuthorizedAsync(userContext, path, method);

        return ok ? Ok() : Forbid();
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