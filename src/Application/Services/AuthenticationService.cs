using System.Web;
using Microsoft.Extensions.Logging;

using OtpNet;

using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;
using DMS.Auth.Domain.Interfaces;

namespace DMS.Auth.Application.Services;

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

    /// Authenticates / Login a user and retrieves a JWT token.
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

    /// Generates TOTP QR Code and Secret 
    public TotpSetupDto GenerateTotpCode(string username, string issuer = "DMS Auth")
    {
        // 1. Generate 20-byte secret
        var secretKey = KeyGeneration.GenerateRandomKey(20);
        string base32Secret = Base32Encoding.ToString(secretKey);

        // 2. Build QR Code URI
        string label = $"{issuer}:{username}";
        string encodedLabel = HttpUtility.UrlEncode(label);
        string encodedIssuer = HttpUtility.UrlEncode(issuer);

        string otpAuthUri = $"otpauth://totp/{encodedLabel}?secret={base32Secret}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";

        // 3. Generate setup token to identify session
        var setupToken = Guid.NewGuid().ToString("N");

        // 4. Store TOTP secret + username in cache
        _cache.StoreSecret(setupToken, new TotpSecretCached
        {
            Username = username,
            Secret = base32Secret,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        });

        return new TotpSetupDto
        {
            Secret = base32Secret,
            QrCodeUri = otpAuthUri,
            Issuer = issuer,
            Username = username,
            SetupToken = setupToken
        };
    }

    /// Verify TOTP QR Code and save Secret 
    public async Task<bool> RegisterTotpAsync(string username, string code, string setupToken)
    {
        // 1. Load the TOTP secret from cache
        var entry = _cache.GetSecret(setupToken);
        if (entry is null || entry.ExpiresAt < DateTime.UtcNow)
            return false;

        // 2. Verify the code against the secret       
        bool isValid = ValidateCode(entry.Secret, code);
        if (!isValid) return false;

        var userId = await _keycloakClient.GetUserIdByUsernameAsync(username);
        if (userId == null) return false;

        // 3. Save the verified secret to the database for this user
        await _secretRepo.AddAsync(userId, entry.Secret);        

        // 5. Clean up setup cache
        _cache.RemoveSecret(setupToken);

        return true;
    }

    /// Verify Login with TOTP Code
    public async Task<LoginResult> VerifyLoginTotpAsync(string setupToken, string code)
    {
        //var userId = await _keycloakClient.GetUserIdByUsernameAsync(username);

        // 1. Get LoginAttempt from cache
        var loginAttempt = _cache.GetLoginAttempt(setupToken);
        if (loginAttempt is null)
            return LoginResult.Fail("Session expired");

        // 2. Load the TOTP secret from DB
        var secret = await _secretRepo.GetAsync(loginAttempt.UserId);
        if (secret is null || string.IsNullOrWhiteSpace(secret))
            return LoginResult.Fail("No Secret in DB for user");

        // 3. Validate the provided 6-digit code
        var isValid = ValidateCode(secret, code);
        if (!isValid)
            return LoginResult.Fail("Invalid TOTP code");

        // 4. Validate credentials via Keycloak & return Token
        var tokenResponse = await _keycloakClient.GetUserAccessTokenAsync(loginAttempt.Username, loginAttempt.Password);

        if (tokenResponse == null || tokenResponse.Access_token == null)
            return LoginResult.Fail("Failed to retrieve token from Keycloak");

        _cache.RemoveLoginAttempt(setupToken);

        return LoginResult.Ok(new
        {            
            setup_token = tokenResponse
        });        
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

    private static bool ValidateCode(string base32Secret, string code)
    {
        try
        {
            var bytes = Base32Encoding.ToBytes(base32Secret);
            var totp = new Totp(bytes);
            return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
        }
        catch
        {
            return false;
        }
    }
}
