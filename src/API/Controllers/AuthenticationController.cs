using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Authentication.Application.Configuration;
using Authentication.Application.Interfaces;
using Authentication.Application.Dtos;
using Authentication.Api.Services;

namespace Authentication.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly AuthSettings _authSettings;

    public AuthenticationController(IAuthenticationService authenticationService, IOptions<AuthSettings> authOptions)
    {
        _authenticationService = authenticationService;
        _authSettings = authOptions.Value;
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

        var result = await _authenticationService.LoginUserAsync(request.Username!, request.Password, request.RememberMe);
        if (result == null || !result.Success)
        {
            return Accepted(result);
        }

        if (string.IsNullOrEmpty(result.Data?.AccessToken) || string.IsNullOrEmpty(result.Data?.RefreshToken))
        {
            return Ok(result);
        }

        AuthCookieService.AppendAuthCookies(Response, Request, result.Data.RefreshToken, result.Data.RememberMe);

        return Ok(result);
    }

    [HttpGet("oauth2callback")]
    public async Task<IActionResult> OAuth2callback([FromQuery] string code)
    {
        var entraIdRedirectUrl = _authSettings.FrontendEntraIdRedirectUri;     

        var result = await _authenticationService.OAuth2CallbackAsync(code);

        if (!string.IsNullOrEmpty(result?.Data?.AccessToken) &&
            !string.IsNullOrEmpty(result?.Data?.RefreshToken))
        {
            AuthCookieService.AppendAuthCookies(Response, Request, result.Data.RefreshToken, result.Data.RememberMe);
        }

        return Redirect(entraIdRedirectUrl);
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

        if (!string.IsNullOrWhiteSpace(result.Data?.Refresh_token))
        {
            AuthCookieService.RefreshAuthCookies(Response, Request, result.Data.Refresh_token);
        }

        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshTokenValue = ExtractRefreshTokenFromCookie(Request.HttpContext);
        if (string.IsNullOrEmpty(refreshTokenValue))
        {
            AuthCookieService.DeleteAuthCookies(Response);
            return Accepted(new { message = "Refresh token is missing" });
        }

        var result = await _authenticationService.LogoutAsync(refreshTokenValue);
        if (!result.Success)
        {
            return Accepted(result);
        }

        AuthCookieService.DeleteAuthCookies(Response);

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
        if (httpContext.Request.Cookies.TryGetValue(AuthCookieService.RefreshTokenCookieName, out var token))
        {
            return token;
        }

        return null;
    }

}