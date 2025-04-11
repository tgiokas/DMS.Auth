using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Authentication.Application.Interfaces;
using Authentication.Application.Dtos;

namespace Authentication.Api.Controllers;

[ApiController]
[Route("api/mfa")]
public class MfaController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public MfaController(IAuthenticationService authenticationService, IUserManagementService userManagementService)
    {
        _authenticationService = authenticationService;
    }   

    /// Fetch TOTP QR Code and Secret for MFA
    [HttpGet("setup")]
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
    [HttpPost("verify-setup")]
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
    [HttpPost("verify-login")]   
    public async Task <IActionResult> VerifyLoginTotpAsync([FromBody] TotpVerifyDto request)
    {
        var response = await _authenticationService.VerifyLoginTotpAsync(request.SetupToken, request.Code);
        if (response == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        return Ok(response);
    }
}
