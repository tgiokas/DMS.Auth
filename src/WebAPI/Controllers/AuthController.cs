using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using DMS.Auth.Application.Interfaces;
using DMS.Auth.Application.Dtos;

namespace DMS.Auth.WebApi;

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

    /// Fetch TOTP QR Code and Secret for MFA
    [HttpGet("mfa/setup")]
    public IActionResult GetTotpAuthCode([FromQuery] string username)
    {       
        var response = _authenticationService.GenerateTotpCode(username);
        if (response == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        return Ok(response);
    }

    /// Verify TOTP Code during SetUp       
    [HttpPost("mfa/verify-setup")]
    public async Task <IActionResult> VerifyAndRegisterTotpAsync([FromBody] TotpVerifyDto request)
    {
        var response = await _authenticationService.RegisterTotpAsync(request.Username, request.Code, request.SetupToken);
        if (response == false)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        return Ok(response);
    }

    /// Validate TOTP Code during Login
    [HttpPost("mfa/verify-login")]   
    public async Task <IActionResult> VerifyLoginTotpAsync([FromBody] TotpVerifyDto request)
    {
        var response = await _authenticationService.VerifyLoginTotpAsync(request.SetupToken, request.Code);
        if (response == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        return Ok(response);
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
