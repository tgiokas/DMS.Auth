using Microsoft.AspNetCore.Mvc;

using DMS.Auth.Application.Interfaces;
using DMS.Auth.Application.Dtos;

namespace DMS.Auth.WebApi;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        var tokenResponse = await _authenticationService.AuthenticateUserAsync(request.Username, request.Password);
        if (tokenResponse == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }
         
        return Ok(tokenResponse);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto request)
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
        var result = await _authenticationService.LogoutAsync(request.RefreshToken);
        if (!result)
        {
            return BadRequest(new { message = "Failed to logout" });
        }

        return Ok(new { message = "Logout successful" });
    }
}
