using Microsoft.AspNetCore.Mvc;

using DMS.Auth.Application.Interfaces;
using DMS.Auth.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;
using Azure.Core;
using Microsoft.AspNetCore.WebUtilities;


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
        var tokenResponse = await _authenticationService.AuthenticateUserAsync(request.Username ?? request.Email ?? string.Empty, request.Password);
        if (tokenResponse == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }
         
        return Ok(tokenResponse);
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
        var result = await _authenticationService.LogoutAsync(request.RefreshToken);
        if (!result)
        {
            return BadRequest(new { message = "Failed to logout" });
        }

        return Ok(new { message = "Logout successful" });
    }

    /// Get Temporary Session Token (`tempToken`)
    /// This allows the user to fetch their TOTP QR code
    [HttpPost("temp-token")]
    public async Task<IActionResult> GetTempToken([FromBody] LoginDto request)
    {
        var tokenResponse = await _authenticationService.GetTempTokenAsync(request.Username ?? request.Email ?? string.Empty, request.Password);
        if (tokenResponse == null)
        {
            return BadRequest("Failed to get Temp Token");
        }
       
        return Ok(tokenResponse);
    }

    /// Fetch TOTP QR Code and Secret for user enrollment 
    [HttpPost("enroll")]
    //[Authorize]  // Requires user to be authenticated
    public async Task<IActionResult> GetMfaAuthCode([FromBody] string tempToken)
    {
        var tokenResponse = await _authenticationService.GetMfaAuthCode(tempToken);
        if (tokenResponse == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        return Ok(tokenResponse);
    }

    /// Verify TOTP Code
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyMfa([FromBody] MfaVerificationRequest request)
    {
        var tokenResponse = await _authenticationService.VerifyMfa(request);
        if (tokenResponse == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        return Ok(tokenResponse);
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
}
