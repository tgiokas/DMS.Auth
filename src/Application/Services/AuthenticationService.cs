using Microsoft.Extensions.Logging;
using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;

namespace DMS.Auth.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IKeycloakClient _keycloakClient;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(IKeycloakClient keycloakClient, ILogger<AuthenticationService> logger)
    {
        _keycloakClient = keycloakClient;
        _logger = logger;
    }

    /// Authenticates a user and retrieves a JWT token.
    public async Task<TokenDto?> AuthenticateUserAsync(string username, string password)
    {
        var tokenResponse = await _keycloakClient.GetUserAccessTokenAsync(username, password);
        if (tokenResponse == null)
        {
            _logger.LogError("Authentication failed for user: {Username}", username);
        }
        return tokenResponse;
    }

    /// Refreshes a user's access token.
    public async Task<TokenDto?> RefreshTokenAsync(string refreshToken)
    {
        var tokenResponse = await _keycloakClient.RefreshTokenAsync(refreshToken);
        if (tokenResponse == null)
        {
            _logger.LogError("Token refresh failed.");
        }
        return tokenResponse;
    }

    public async Task<TokenTempDto?> GetTempTokenAsync(string username, string password)
    {
        var tokenResponse = await _keycloakClient.GetTempTokenAsync(username, password);
        if (tokenResponse == null)
        {
            _logger.LogError("Token refresh failed.");
        }
        return tokenResponse;
    }

    /// Fetch TOTP QR Code and Secret for user enrollment 
    public async Task<MfaEnrollmentResponse?> GetMfaAuthCode(string tempToken)
    {
        var tokenResponse = await _keycloakClient.GetMfaAuthCode(tempToken);
        if (tokenResponse == null)
        {
            _logger.LogError("Token refresh failed.");
        }
        return tokenResponse;
    }

    public async Task<TokenDto?> VerifyMfa(MfaVerificationRequest request)
    {
        var tokenResponse = await _keycloakClient.VerifyMfaAuthCode(request);
        if (tokenResponse == null)
        {
            _logger.LogError("Token refresh failed.");
        }
        return tokenResponse;
    }

    /// Logs out a user by invalidating their refresh token.
    public async Task<bool> LogoutAsync(string refreshToken)
    {
        return await _keycloakClient.LogoutAsync(refreshToken);
    }

    public async Task<string?> LoginWithGsis()
    {
        var tokenResponse = await _keycloakClient.GsisLoginUrl();
        if (tokenResponse == null)
        {
            _logger.LogError("Login failed");
        }
        return tokenResponse;
    }

    public async Task<TokenDto?> GsisCallback(string code)
    {
        var tokenResponse = await _keycloakClient.GsisCallback(code);
        if (tokenResponse == null)
        {
            _logger.LogError("Authentication failed for user: {Username}", code);
        }
        return tokenResponse;
    }
}
