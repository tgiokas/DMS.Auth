using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Authentication.Application.Interfaces;
using Authentication.Application.Dtos;

namespace Authentication.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthController(IAuthenticationService authenticationService, IUserManagementService userManagementService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        var response = await _authenticationService.LoginUserAsync(request.Username ?? request.Email ?? string.Empty, request.Password);
        if (response == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }       

        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] TokenRefreshDto request)
    {
        var tokenResponse = await _authenticationService.RefreshTokenAsync(request.RefreshToken);
        if (tokenResponse == null)
        {
            return Unauthorized(new { message = "Invalid refresh token" });
        }

        return Ok(tokenResponse);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutDto request)
    {
        var response = await _authenticationService.LogoutAsync(request.RefreshToken);
        if (!response)
        {
            return BadRequest(new { message = "Failed to logout" });
        }

        return Ok(new { message = "Logout successful" });
    }
       

    //[HttpGet("gsis-login")]
    //public async Task<IActionResult> LoginWithGsis()
    //{
    //    var loginUrl = await _authenticationService.LoginWithGsis();

    //    return Redirect(loginUrl);
    //}

    //[HttpGet("gsis-callback")]
    //public async Task<IActionResult> GsisCallback([FromQuery] string code)
    //{
    //    var tokenResponse = await _authenticationService.GsisCallback(code);
    //    if (tokenResponse == null)
    //    {
    //        return Unauthorized(new { message = "Invalid refresh token" });
    //    }

    //    return Ok(tokenResponse);
    //}

    //[HttpGet("token-info")]
    //[Authorize] // Requires valid JWT Token
    //public IActionResult GetTokenInfo()
    //{
    //    var identity = User.Identity as ClaimsIdentity;

    //    if (identity == null)
    //        return Unauthorized("No identity found.");

    //    var claims = identity.Claims
    //        .Select(c => new { c.Type, c.Value })
    //        .ToList();

    //    Console.WriteLine("===== Extracted Claims =====");
    //    foreach (var claim in claims)
    //    {
    //        Console.WriteLine($"- {claim.Type}: {claim.Value}");
    //    }
    //    Console.WriteLine("============================");

    //    return Ok(claims);
    //}
}
