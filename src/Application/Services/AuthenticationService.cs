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
    public async Task<string?> AuthenticateUserAsync(string username, string password)
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

    /// Logs out a user by invalidating their refresh token.
    public async Task<bool> LogoutAsync(string refreshToken)
    {
        return await _keycloakClient.LogoutAsync(refreshToken);
    }
}
