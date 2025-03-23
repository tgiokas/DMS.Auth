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

    /// Fetch TOTP QR Code and Secret for user enrollment 
    [HttpGet("mfa/setup")]
    //[Authorize]  // Requires user to be authenticated
    public IActionResult GetMfaAuthCode([FromQuery] string username)
    {       
        var tokenResponse = _authenticationService.GenerateMfaAuthCode(username);
        if (tokenResponse == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        return Ok(tokenResponse);
    }

    /// Verify TOTP Code
    /// TODO Protect the /verify-totp Endpoint with a Temporary Token / Session    
    [HttpPost("mfa/verify")]
    public IActionResult VerifyMfaCode([FromBody] MfaVerifyDto request)
    {
        var tokenResponse = _authenticationService.VerifyAndRegisterTotpAsync(request.Username, request.OtpCode);
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
