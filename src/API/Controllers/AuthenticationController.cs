using Microsoft.AspNetCore.Mvc;

using Authentication.Api.Constants;
using Authentication.Application.Interfaces;
using Authentication.Application.Dtos;

namespace Authentication.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthenticationController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto request)
    {
        if (string.IsNullOrEmpty(request.Username) && string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new { message = "Either Username or Email must be provided." });
        }

        if (string.IsNullOrEmpty(request.Username) && !string.IsNullOrEmpty(request.Email))
        {
            request.Username = request.Email;
        }

        var result = await _authenticationService.LoginUserAsync(request.Username!, request.Password);
        if (result == null || !result.Success)
        {
            return Accepted(result);
        }

        if (string.IsNullOrEmpty(result?.Data?.AccessToken) || string.IsNullOrEmpty(result?.Data?.RefreshToken))
        {
            return Ok(result);
        }
        else
        {
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

    [HttpGet("oauth2callback")]
    public async Task<IActionResult> OAuth2callback([FromQuery] string code)
    {        
        var result = await _authenticationService.OAuth2CallbackAsync(code);
        if (result == null || string.IsNullOrEmpty(result?.Data?.AccessToken))
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshTokenValue = ExtractRefreshTokenFromCookie(Request.HttpContext);
        if (string.IsNullOrEmpty(refreshTokenValue))
        {
            return Accepted(new { message = "Refresh token is missing" });
        }

        var result = await _authenticationService.RefreshTokenAsync(refreshTokenValue);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshTokenValue = ExtractRefreshTokenFromCookie(Request.HttpContext);
        if (string.IsNullOrEmpty(refreshTokenValue))
        {
            return Accepted(new { message = "Refresh token is missing" });
        }

        var result = await _authenticationService.LogoutAsync(refreshTokenValue);
        if (!result.Success)
        {
            return Accepted(result);
        }

        Response.Cookies.Delete("refresh_token");

        return Ok(result);
    }

    public static string? ExtractAccessToken(HttpContext httpContext)
    {
        var authorizationHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
        {
            return null;
        }

        return authorizationHeader.Split(" ").Last();
    }

    private static string? ExtractRefreshTokenFromCookie(HttpContext httpContext)
    {
        if (httpContext.Request.Cookies.TryGetValue("refresh_token", out var token))
        {
            return token;
        }

        return null;
    }
}
