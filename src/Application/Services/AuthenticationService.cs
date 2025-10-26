using Microsoft.Extensions.Configuration;

using Authentication.Application.Dtos;
using Authentication.Application.Errors;
using Authentication.Application.Interfaces;
using Authentication.Domain.Enums;
using Authentication.Domain.Interfaces;

namespace Authentication.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IKeycloakClientAuthentication _keycloakClientAuth;
    private readonly IKeycloakClientUser _keycloakClientUser;
    private readonly ITotpRepository _secretRepo;
    private readonly ITotpCache _cache;
    private readonly IConfiguration _configuration;
    private readonly IErrorCatalog _errors;

    public AuthenticationService(
        IKeycloakClientAuthentication keycloakClientAuth,
        IKeycloakClientUser keycloakClientUser,
        ITotpRepository secretRepo,
        ITotpCache cache,
        IConfiguration configuration,
        IErrorCatalog errors)
    {
        _keycloakClientAuth = keycloakClientAuth;
        _keycloakClientUser = keycloakClientUser;
        _secretRepo = secretRepo;
        _cache = cache;
        _configuration = configuration;
        _errors = errors;
    }

    /// Authenticate / Login a user and retrieves a JWT token.
    public async Task<Result<LoginResponseDto>?> LoginUserAsync(string username, string password)
    {
        // 1. Validate credentials via Keycloak & return Token  
        var tokenResponse = await _keycloakClientAuth.GetUserAccessTokenAsync(username, password);
        if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.Access_token))
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.AuthenticationFailed);
        }

        var keycloakUser = await _keycloakClientUser.GetUserByNameAsync(username);
        if (keycloakUser == null && username.Contains('@'))
        {
            keycloakUser = await _keycloakClientUser.GetUserByEmailAsync(username);
        }
        if (keycloakUser == null) return null;

        if (!Guid.TryParse(keycloakUser.Id, out var userId))
            return null;

        // 2. Check MFA type from configuration
        var mfaTypeString = _configuration["MfaType"] ?? "none";
        if (!Enum.TryParse<MfaType>(mfaTypeString, true, out var mfaType))
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.InvalidMfaType);
        }

        // 3. No MFA --> return token directly  
        if (mfaType == MfaType.None)
        {
            return Result<LoginResponseDto>.Ok(new LoginResponseDto
            {
                MfaEnabled = false,
                MfaMethod = MfaType.None.ToString().ToLower(),
                AccessToken = tokenResponse.Access_token,
                RefreshToken = tokenResponse.Refresh_token,
                ExpiresIn = tokenResponse.Expires_in
            });
        }
        else
        {
            // 4. With MFA --> check if TOTP is configured
            var hasTotp = await _secretRepo.ExistsAsync(userId);

            // 5. Generate setup token and store LoginAttempt in cache  
            var setupToken = Guid.NewGuid().ToString("N");
            await _cache.StoreLoginAttemptAsync(setupToken, new LoginAttemptCached
            {
                Username = username,
                Password = password,
                KeycloakUserId = userId,
                Email = keycloakUser.Email,
            });

            return Result<LoginResponseDto>.Ok(new LoginResponseDto
            {
                MfaEnabled = mfaType switch
                {
                    MfaType.Email or MfaType.Sms => true,
                    MfaType.Totp => hasTotp,
                    _ => false
                },
                MfaMethod = mfaType.ToString().ToLower(),
                MfaSetUpToken = setupToken
            });
        }
    }

    public async Task<Result<RefreshResponseDto>> RefreshTokenAsync(string refreshToken)
    {
        var tokenResponse = await _keycloakClientAuth.RefreshTokenAsync(refreshToken);
        if (tokenResponse == null || tokenResponse.Access_token == null)
        {
            return _errors.Fail<RefreshResponseDto>(ErrorCodes.AUTH.RefreshFailed);
        }

        return Result<RefreshResponseDto>.Ok(new RefreshResponseDto
        {
            Access_token = tokenResponse.Access_token,
            Expires_in = tokenResponse.Expires_in
        });
    }

    public async Task<Result<bool>> LogoutAsync(string refreshToken)
    {
        var result = await _keycloakClientAuth.LogoutAsync(refreshToken);
        if (!result)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.LogoutFailed);
        }

        return Result<bool>.Ok(result, message: "Logout successful");
    }

    /// Authenticate an OAuth2 callback Code and retrieves a JWT token.
    public async Task<Result<LoginResponseDto>> OAuth2CallbackAsync(string code)
    {
        var tokenResponse = await _keycloakClientAuth.GetAccessTokenByCodeAsync(code);
        if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.Access_token))
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.AuthenticationFailed);
        }

        return Result<LoginResponseDto>.Ok(new LoginResponseDto
        {
            MfaEnabled = false,
            AccessToken = tokenResponse.Access_token,
            RefreshToken = tokenResponse.Refresh_token,
            ExpiresIn = tokenResponse.Expires_in
        });
    }
}