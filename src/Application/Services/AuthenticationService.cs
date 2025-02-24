using Microsoft.Extensions.Logging;

using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;

public class AuthenticationService : IAuthenticationService
{
    private readonly IKeycloakClient _keycloakClient;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(IKeycloakClient keycloakClient, ILogger<AuthenticationService> logger)
    {
        _keycloakClient = keycloakClient;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and retrieves a JWT token.
    /// </summary>
    public async Task<TokenResponse?> AuthenticateUserAsync(string username, string password)
    {
        var tokenResponse = await _keycloakClient.GetTokenAsync(username, password);
        if (tokenResponse == null)
        {
            _logger.LogError("Authentication failed for user: {Username}", username);
        }
        return tokenResponse;
    }

    /// <summary>
    /// Refreshes a user's access token.
    /// </summary>
    public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        var tokenResponse = await _keycloakClient.RefreshTokenAsync(refreshToken);
        if (tokenResponse == null)
        {
            _logger.LogError("Token refresh failed.");
        }
        return tokenResponse;
    }

    /// <summary>
    /// Logs out a user by invalidating their refresh token.
    /// </summary>
    public async Task<bool> LogoutAsync(string refreshToken)
    {
        return await _keycloakClient.LogoutAsync(refreshToken);
    }
}
