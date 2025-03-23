using Microsoft.Extensions.Logging;
using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;
using System.Web;
using OtpNet;

namespace DMS.Auth.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IKeycloakClient _keycloakClient;    
    private readonly ITotpCacheService _cache;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(IKeycloakClient keycloakClient, ITotpCacheService cache, ILogger<AuthenticationService> logger)
    {
        _keycloakClient = keycloakClient;
        _cache = cache;
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


    /// Generates TOTP QR Code and Secret for user enrollment 
    public MfaSecretDto GenerateMfaAuthCode(string username, string issuer = "DMS Auth")
    {
        // Generate a 160-bit (20-byte) TOTP secret key
        var secretKey = KeyGeneration.GenerateRandomKey(20);
        string base32Secret = Base32Encoding.ToString(secretKey);

        // Build otpauth URI for QR code
        string label = $"{issuer}:{username}";
        string encodedLabel = HttpUtility.UrlEncode(label);
        string encodedIssuer = HttpUtility.UrlEncode(issuer);

        string otpAuthUri =
            $"otpauth://totp/{encodedLabel}?secret={base32Secret}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";

        _cache.StoreSecret(username, base32Secret);

        return new MfaSecretDto
        {
            Secret = base32Secret,
            QrCodeUri = otpAuthUri,
            Issuer = issuer,
            Username = username
        };
    }

    public async Task<bool> VerifyAndRegisterTotpAsync(string username, string code)
    {
        var base32Secret = _cache.GetSecret(username);
        if (string.IsNullOrWhiteSpace(base32Secret))
            throw new InvalidOperationException("TOTP secret not found or expired.");

        var totp = new Totp(Base32Encoding.ToBytes(base32Secret));       
        bool isValid = totp.VerifyTotp(code, out _, new VerificationWindow(previous: 2, future: 2));

        if (!isValid) return false;

        var userId = await _keycloakClient.GetUserIdByUsernameAsync(username);
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("User not found in Keycloak.");

        bool stored = await _keycloakClient.StoreTotpCredentialAsync(userId, base32Secret);
        if (!stored)
            throw new Exception("Failed to store TOTP credential in Keycloak.");

        _cache.RemoveSecret(username);
        return true;
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
