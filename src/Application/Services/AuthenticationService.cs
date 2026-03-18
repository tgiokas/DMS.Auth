using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;

using Authentication.Application.Dtos;
using Authentication.Application.Errors;
using Authentication.Application.Interfaces;
using Authentication.Domain.Enums;
using Authentication.Domain.Interfaces;
using Authentication.Domain.Entities;

namespace Authentication.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IKeycloakClientAuthentication _keycloakClientAuth;
    private readonly IKeycloakClientUser _keycloakClientUser;
    private readonly IAuthLockoutService _authLockout;
    private readonly IUserRepository _userRepository;
    private readonly ITotpRepository _secretRepo;
    private readonly IEmailWhitelistRepository _emailWhitelistRepo;
    private readonly ITotpCache _cache;
    private readonly IConfiguration _configuration;
    private readonly IErrorCatalog _errors;

    public AuthenticationService(
        IKeycloakClientAuthentication keycloakClientAuth,
        IKeycloakClientUser keycloakClientUser,       
        IAuthLockoutService authLockout,
        IUserRepository userRepository,
        ITotpRepository secretRepo,
        IEmailWhitelistRepository emailWhitelistRepo,
        ITotpCache cache,
        IConfiguration configuration,
        IErrorCatalog errors)
    {
        _keycloakClientAuth = keycloakClientAuth;
        _keycloakClientUser = keycloakClientUser; 
        _authLockout = authLockout;
        _userRepository = userRepository;
        _secretRepo = secretRepo;
        _emailWhitelistRepo = emailWhitelistRepo;
        _cache = cache;
        _configuration = configuration;
        _errors = errors;
    }

    /// Authenticate / Login a user and retrieves a JWT token.
    public async Task<Result<LoginResponseDto>?> LoginUserAsync(string username, string password)
    {
        var loginKey = $"pwd:{username.Trim().ToLowerInvariant()}";

        // Check lock
        if (await _authLockout.IsLockedAsync(loginKey))
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.TooManyAttempts);
        }

        // Validate credentials via Keycloak & return Token  
        var tokenResponse = await _keycloakClientAuth.GetUserAccessTokenAsync(username, password);
        if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.Access_token))
        {
            // Register failure
            await _authLockout.RegisterLoginFailureAsync(loginKey);
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.AuthenticationFailed);
        }

        // Register success
        await _authLockout.RegisterLoginSuccessAsync(loginKey);

        var keycloakUser = await _keycloakClientUser.GetUserByNameAsync(username);
        if (keycloakUser == null && username.Contains('@'))
        {
            keycloakUser = await _keycloakClientUser.GetUserByEmailAsync(username);
        }
        if (keycloakUser == null) return null;

        if (!Guid.TryParse(keycloakUser.Id, out var userId))
            return null;

        // Check MFA type from repository
        var dbUser = await _userRepository.GetByKeycloakUserIdAsync(userId);
        if (dbUser == null)
            return null;

        // No MFA --> return token directly  
        if (dbUser.MfaType == MfaType.None)
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
            // With MFA --> check if TOTP is configured
            var hasTotp = await _secretRepo.ExistsAsync(userId);

            // Generate setup token and store LoginAttempt in cache  
            var setupToken = Guid.NewGuid().ToString("N");
            await _cache.StoreLoginAttemptAsync(setupToken, new LoginAttemptCached
            {
                Username = username,
                KeycloakUserId = userId,
                Email = keycloakUser.Email,
                
                AccessToken = tokenResponse.Access_token??"",
                RefreshToken = tokenResponse?.Refresh_token??"",
                ExpiresIn = tokenResponse?.Expires_in??0
            });

            return Result<LoginResponseDto>.Ok(new LoginResponseDto
            {
                MfaEnabled = dbUser.MfaType switch
                {
                    MfaType.Email or MfaType.Sms => true,
                    MfaType.Totp => hasTotp,
                    _ => false
                },
                MfaMethod = dbUser.MfaType.ToString().ToLower(),
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

        if (string.IsNullOrWhiteSpace(tokenResponse.Refresh_token))
        {
            return _errors.Fail<RefreshResponseDto>(ErrorCodes.AUTH.RefreshFailed);
        }

        return Result<RefreshResponseDto>.Ok(new RefreshResponseDto
        {
            Access_token = tokenResponse.Access_token,
            Refresh_token = tokenResponse.Refresh_token,
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

        // Parse email from JWT token
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenResponse.Access_token);
        var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        if (email == null)
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.AuthenticationFailed);
        }

        // Check email whitelist if enabled
        var whitelistTypeValue = _configuration["AUTH_EMAILS_WHITELIST"];
        if (string.IsNullOrWhiteSpace(whitelistTypeValue))
            throw new ArgumentNullException(nameof(_configuration), "AUTH_EMAILS_WHITELIST is null.");

        if (!whitelistTypeValue.Equals("off", StringComparison.CurrentCultureIgnoreCase))
        {
            var isWhitelisted = await _emailWhitelistRepo.IsWhitelistedAsync(email);
            if (!isWhitelisted)
            {
                return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.EmailNotWhitelisted);
            }
        }
        
        var keycloakUser = await _keycloakClientUser.GetUserByEmailAsync(email);
        if (keycloakUser == null)
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.AuthenticationFailed);
        }

        if (!Guid.TryParse(keycloakUser.Id, out var keycloakUserId))
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.UserIdNotValid);

        var updateDto = new KeycloakUserDto
        {
            Id = keycloakUser.Id,
            EmailVerified = true,
        };

        var keycloakUpdateResult = await _keycloakClientUser.UpdateUserAsync(updateDto);
        if (!keycloakUpdateResult.Success)
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.UpdateInKeycloakFailed);
        }

        // Ensure local user exists
        var localUser = await _userRepository.GetByKeycloakUserIdAsync(keycloakUserId);
        if (localUser == null)
        {
            await _userRepository.AddAsync(new User
            {
                KeycloakUserId = keycloakUserId,
                Username = keycloakUser.UserName,
                IsAdmin = false,
                MfaType = MfaType.None,
            });
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