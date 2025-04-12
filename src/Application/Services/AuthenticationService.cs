using Microsoft.Extensions.Logging;

using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;
using Authentication.Domain.Interfaces;

namespace Authentication.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IKeycloakClient _keycloakClient;    
    private readonly ITotpCacheService _cache;
    private readonly ITotpRepository _secretRepo;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(IKeycloakClient keycloakClient,
        ITotpCacheService cache,
        ITotpRepository secretRepo,
        ILogger<AuthenticationService> logger)
    {
        _keycloakClient = keycloakClient;
        _cache = cache;
        _secretRepo = secretRepo;
        _logger = logger;
    }

    /// Authenticate / Login a user and retrieves a JWT token.
    public async Task<LoginResult?> LoginUserAsync(string username, string password)
    {
        // 1. Validate credentials via Keycloak token endpoint
        var tokenResponse = await _keycloakClient.GetUserAccessTokenAsync(username, password);
        if (tokenResponse == null || tokenResponse.Access_token == null)
        {            
            return LoginResult.Fail("Authentication failed for user");
        }       

        // 2. Check if MFA is required
        var userId = await _keycloakClient.GetUserIdByUsernameAsync(username);
        if (string.IsNullOrEmpty(userId))
            return null;
        var hasTotp = await _secretRepo.ExistsAsync(userId);

        // 3.No TOTP user has no MFA setup  return token directly
        if (!hasTotp)
        {
            return LoginResult.Ok(new
            {
                mfa_required = false,
                token_response = tokenResponse
            });            
        }
        else
        {
            // 4. Generate setup token and store LoginAttempt in cache
            var setupToken = Guid.NewGuid().ToString("N");
            _cache.StoreLoginAttempt(setupToken, new LoginAttemptCached
            {
                Username = username,
                Password = password,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            });

            return LoginResult.Ok(new
            {
                mfa_required = true,
                setup_token = setupToken
            });
        }
    }        

    public async Task<TokenDto?> RefreshTokenAsync(string refreshToken)
    {
        var tokenResponse = await _keycloakClient.RefreshTokenAsync(refreshToken);        
        return tokenResponse;
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        return await _keycloakClient.LogoutAsync(refreshToken);
    }

    public async Task<string?> LoginWithGsis()
    {
        var response = await _keycloakClient.GsisLoginUrl();        
        return response;
    }

    public async Task<TokenDto?> GsisCallback(string code)
    {
        var response = await _keycloakClient.GsisCallback(code);       
        return response;
    }
    
}
