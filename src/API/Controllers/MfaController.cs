using Microsoft.AspNetCore.Mvc;

using Authentication.Api.Constants;
using Authentication.Application.Interfaces;
using Authentication.Application.Dtos;

namespace Authentication.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class MfaController : ControllerBase
{
    private readonly IMfaService _mfaService;

    public MfaController(IMfaService mfaService)
    {
        _mfaService = mfaService;        
    }

    /// Set MFA Type to user 
    [HttpPost("set-type")]
    public async Task<IActionResult> SetMfaType(UserMfaTypeDto request)
    {
        var result = await _mfaService.SetMfaTypeAsync(request.Username, request.MfaType);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    /// Fetch TOTP QR Code and Secret for MFA
    [HttpPost("setup-totp")]
    public async Task<IActionResult> GetTotpAuthCode(UserNameDto request)
    {
        var result = await _mfaService.GenerateTotpCodeAsync(request.Username);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    /// Verify TOTP Code during SetUp       
    [HttpPost("verify-setup")]
    public async Task <IActionResult> VerifyAndRegisterTotpAsync(TotpRegisterDto request)
    {
        var result = await _mfaService.RegisterTotpAsync(request.Username, request.Code, request.SetupToken);
        if (!result.Success)
        {
            return Accepted(result);
        }        

        return Ok(result);
    }

    /// Validate TOTP Code during Login
    [HttpPost("verify-totp")]
    public async Task<IActionResult> VerifyLoginTotpAsync(TotpVerifyDto request)
    {
        var result = await _mfaService.VerifyLoginByTotpAsync(request.LoginToken, request.Code);
        if (!result.Success)
        {
            return Accepted(result);
        }

        if (string.IsNullOrEmpty(result?.Data?.AccessToken) || string.IsNullOrEmpty(result?.Data?.RefreshToken))
        {
            return Ok(result);
        }

        Response.Cookies.Append("refresh_token", result.Data.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddHours(CookieConstants.RefreshTokenCookieExpirationHours)
        });

        return Ok(result);
    }

    /// Disable TOTP       
    [HttpPost("disable-totp")]
    public async Task<IActionResult> DisableTotpAsync(UserNameDto request)
    {
        var result = await _mfaService.DisableTotpAsync(request.Username);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }
    /// Send MFA Email Code 
    [HttpPost("send-email")]
    public async Task<IActionResult> SendEmailCode(TotpLoginTokenDto request)
    {
        var result = await _mfaService.SendEmailCodeAsync(request.LoginToken);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    /// Validate Login with Email Code
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyLoginEmailAsync(TotpVerifyDto request)
    {
        var result = await _mfaService.VerifyLoginByEmailAsync(request.LoginToken, request.Code);
        if (!result.Success)
        {
            return Accepted(result);
        }

        if (string.IsNullOrEmpty(result?.Data?.AccessToken) || string.IsNullOrEmpty(result?.Data?.RefreshToken))
        {
            return Ok(result);
        }

        Response.Cookies.Append("refresh_token", result.Data.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddHours(CookieConstants.RefreshTokenCookieExpirationHours)
        });

        return Ok(result);
    }

    /// Send MFA Sms Code 
    [HttpPost("send-sms")]
    public async Task<IActionResult> SendSmsCode(TotpLoginTokenDto request)
    {
        var result = await _mfaService.SendSmsCodeAsync(request.LoginToken);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    /// Validate Login with SMS Code
    [HttpPost("verify-sms")]
    public async Task<IActionResult> VerifyLoginSmsAsync(TotpVerifyDto request)
    {
        var result = await _mfaService.VerifyLoginBySmsAsync(request.LoginToken, request.Code);
        if (!result.Success)
        {
            return Accepted(result);
        }

        if (string.IsNullOrEmpty(result?.Data?.AccessToken) || string.IsNullOrEmpty(result?.Data?.RefreshToken))
        {
            return Ok(result);
        }

        Response.Cookies.Append("refresh_token", result.Data.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddHours(CookieConstants.RefreshTokenCookieExpirationHours)
        });

        return Ok(result);
    }
}
