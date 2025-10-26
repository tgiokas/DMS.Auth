using System.Web;

using OtpNet;

using Authentication.Application.Dtos;
using Authentication.Application.Errors;
using Authentication.Application.Interfaces;
using Authentication.Domain.Enums;
using Authentication.Domain.Entities;
using Authentication.Domain.Interfaces;

namespace Authentication.Application.Services;

public class MfaService : IMfaService
{
    private readonly IUserManagementService _userService;
    private readonly ISmsVerificationService _smsVerification;
    private readonly IEmailVerificationService _emailVerification;
    private readonly IKeycloakClientAuthentication _keycloakClientAuth;
    private readonly IKeycloakClientUser _keycloakClientUser;
    private readonly ITotpRepository _secretRepo;
    private readonly ITotpCache _totpCache;
    private readonly IErrorCatalog _errors;

    public MfaService(
        IUserManagementService userService,
        ISmsVerificationService smsVerification,
        IEmailVerificationService emailVerification,
        IKeycloakClientAuthentication keycloakClientAuth,
        IKeycloakClientUser keycloakClientUser,
        ITotpRepository secretRepo,
        ITotpCache cache,
        IErrorCatalog errors)
    {
        _userService = userService;
        _smsVerification = smsVerification;
        _emailVerification = emailVerification;
        _keycloakClientAuth = keycloakClientAuth;
        _keycloakClientUser = keycloakClientUser;
        _secretRepo = secretRepo;
        _totpCache = cache;
        _errors = errors;
    }

    /// Generate TOTP QR Code and Secret 
    public async Task<Result<TotpSetupDto>> GenerateTotpCode(string username, string issuer = "Auth")
    {
        var keycloakUser = await _keycloakClientUser.GetUserByNameAsync(username);
        if (keycloakUser == null)
        {
            return _errors.Fail<TotpSetupDto>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        var secret = await _secretRepo.GetAsync(Guid.Parse(keycloakUser.Id));
        if (secret is not null)
        {
            return _errors.Fail<TotpSetupDto>(ErrorCodes.AUTH.TOTPExists);
        }

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
        await _totpCache.StoreSecretAsync(setupToken, new TotpSecretCached
        {
            Username = username,
            Secret = base32Secret,
        });

        var topSetup = new TotpSetupDto
        {
            Secret = base32Secret,
            QrCodeUri = otpAuthUri,
            Issuer = issuer,
            Username = username,
            SetupToken = setupToken
        };

        return Result<TotpSetupDto>.Ok(data: topSetup, message: "Totp registered");
    }

    /// Verify TOTP QR Code and save Secret 
    public async Task<Result<bool>> RegisterTotpAsync(string username, string code, string setupToken)
    {
        var keycloakUser = await _keycloakClientUser.GetUserByNameAsync(username);
        if (keycloakUser == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        var secret = await _secretRepo.GetAsync(Guid.Parse(keycloakUser.Id));
        if (secret is not null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.TOTPExists);
        }

        // 1. Load the TOTP secret from cache
        var entry = await _totpCache.GetSecretAsync(setupToken);
        if (entry is null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.LoginSessionExpired);
        }

        // 2. Verify the code against the secret       
        bool isValid = ValidateCode(entry.Secret, code);
        if (!isValid)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.InvalidTOTPcode);
        }

        // 3. Save the verified secret to the DB for this user
        await _secretRepo.AddAsync(new UserTotpSecret
        {
            KeycloakUserId = Guid.Parse(keycloakUser.Id),
            Base32Secret = entry.Secret,
            Enabled = true,
            Verified = true,
            LastVerifiedAt = DateTime.UtcNow
        });

        // 4. Clean up setup cache
        await _totpCache.RemoveSecretAsync(setupToken);

        // 5. Enable MFA for the user in DB
        await _userService.EnableMfaAsync(username, MfaType.Totp);

        return Result<bool>.Ok(data: true, message: "Totp registered");
    }

    /// Disable or reset user's TOTP secret.
    public async Task<Result<bool>> DisableTotpAsync(string username)
    {
        var keycloakUser = await _keycloakClientUser.GetUserByNameAsync(username);
        if (keycloakUser == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        var userId = Guid.Parse(keycloakUser.Id);
        var exists = await _secretRepo.ExistsAsync(userId);

        if (!exists)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.NoSecretInDBForUser);
        }

        await _secretRepo.DeleteAsync(userId);
        await _userService.DisableMfaAsync(username);

        return Result<bool>.Ok(true, message: "TOTP disabled successfully");
    }

    /// Validate Login with TOTP Code  
    public async Task<Result<LoginResponseDto>> VerifyLoginByTotpAsync(string setupToken, string code)
    {
        // 1. Get LoginAttempt from cache  
        var loginAttempt = await _totpCache.GetLoginAttemptAsync(setupToken);
        if (loginAttempt is null)
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.LoginSessionExpired);
        }

        // 2. Load the TOTP secret from DB  
        var secret = await _secretRepo.GetByUserIdAsync(loginAttempt.KeycloakUserId);
        if (secret == null || string.IsNullOrWhiteSpace(secret.Base32Secret))
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.NoSecretInDBForUser);
        }

        // 3. Validate the provided 6-digit code  
        var isValid = ValidateCode(secret.Base32Secret, code);
        if (!isValid)
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.InvalidTOTPcode);
        }

        // 4. Validate credentials via Keycloak & return Token  
        var tokenResponse = await _keycloakClientAuth.GetUserAccessTokenAsync(loginAttempt.Username, loginAttempt.Password);
        if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.Access_token))
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.AuthenticationFailed);
        }

        // 5. Clean up LoginAttempt cache  
        await _totpCache.RemoveLoginAttemptAsync(setupToken);

        // 6. Update the last verified timestamp for the TOTP secret
        secret.LastVerifiedAt = DateTime.UtcNow;
        await _secretRepo.UpdateAsync(secret);

        var loginResponse = new LoginResponseDto
        {
            MfaEnabled = true,
            MfaMethod = MfaType.Totp.ToString().ToLower(),
            AccessToken = tokenResponse.Access_token,
            RefreshToken = tokenResponse.Refresh_token,
            ExpiresIn = tokenResponse.Expires_in
        };

        return Result<LoginResponseDto>.Ok(loginResponse);
    }

    /// Send MFA Email Code 
    public async Task<Result<bool>> SendEmailCodeAsync(string setupToken)
    {
        var loginAttempt = await _totpCache.GetLoginAttemptAsync(setupToken);
        if (loginAttempt is null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.LoginSessionExpired);
        }

        if (string.IsNullOrWhiteSpace(loginAttempt.Email))
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.NoEmailAvailableForMFA);
        }

        var result = await _emailVerification.SendMfaCodeAsync(loginAttempt.Email!);
        if (!result.Success)
        {
            return _errors.Fail<bool>(result.ErrorCode!);
        }

        return Result<bool>.Ok(data: true, message: "Email Code Sent");
    }

    /// Validate Login with Email Code
    public async Task<Result<LoginResponseDto>> VerifyLoginByEmailAsync(string setupToken, string code)
    {
        var loginAttempt = await _totpCache.GetLoginAttemptAsync(setupToken);
        if (loginAttempt is null)
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.LoginSessionExpired);
        }

        var result = await _emailVerification.VerifyMfaCodeAsync(loginAttempt.Email!, code);
        if (!result.Success)
        {
            return _errors.Fail<LoginResponseDto>(result.ErrorCode!);
        }

        var tokenResponse = await _keycloakClientAuth.GetUserAccessTokenAsync(loginAttempt.Username, loginAttempt.Password);
        if (tokenResponse == null || tokenResponse.Access_token == null)
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.AuthenticationFailed);
        }

        await _totpCache.RemoveLoginAttemptAsync(setupToken);

        await _userService.EnableMfaAsync(loginAttempt.Username, MfaType.Email);

        var loginResponse = new LoginResponseDto
        {
            MfaEnabled = true,
            MfaMethod = MfaType.Email.ToString().ToLower(),
            AccessToken = tokenResponse.Access_token,
            RefreshToken = tokenResponse.Refresh_token,
            ExpiresIn = tokenResponse.Expires_in
        };

        return Result<LoginResponseDto>.Ok(loginResponse);
    }

    /// Send MFA Sms Code 
    public async Task<Result<bool>> SendSmsCodeAsync(string setupToken)
    {
        var loginAttempt = await _totpCache.GetLoginAttemptAsync(setupToken);
        if (loginAttempt is null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.LoginSessionExpired);
        }

        if (string.IsNullOrWhiteSpace(loginAttempt.PhoneNumber))
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.InvalidPhone);
        }

        var result = await _smsVerification.SendMfaSmsAsync(loginAttempt.PhoneNumber);
        if (!result.Success)
        {
            return _errors.Fail<bool>(result.ErrorCode!);
        }

        return Result<bool>.Ok(data: true, message: "Sms Code Sent");
    }

    /// Validate Login with SMS Code
    public async Task<Result<LoginResponseDto>> VerifyLoginBySmsAsync(string setupToken, string code)
    {
        var loginAttempt = await _totpCache.GetLoginAttemptAsync(setupToken);
        if (loginAttempt is null)
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.LoginSessionExpired);
        }

        var isValid = _smsVerification.VerifyMfaCode(loginAttempt.PhoneNumber!, code);
        if (!isValid)
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.InvalidSmsCode);
        }

        var tokenResponse = await _keycloakClientAuth.GetUserAccessTokenAsync(loginAttempt.Username, loginAttempt.Password);
        if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.Access_token))
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.AuthenticationFailed);
        }

        await _totpCache.RemoveLoginAttemptAsync(setupToken);

        await _userService.EnableMfaAsync(loginAttempt.Username, MfaType.Sms);

        var loginResponse = new LoginResponseDto
        {
            MfaEnabled = true,
            MfaMethod = MfaType.Sms.ToString().ToLower(),
            AccessToken = tokenResponse.Access_token,
            RefreshToken = tokenResponse.Refresh_token,
            ExpiresIn = tokenResponse.Expires_in
        };

        return Result<LoginResponseDto>.Ok(loginResponse);
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
