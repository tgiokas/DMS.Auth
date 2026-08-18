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
    private readonly ISmsVerificationService _smsVerification;
    private readonly IEmailVerificationService _emailVerification;
    private readonly IKeycloakClientUser _keycloakClientUser;
    private readonly IKeycloakClientAuthentication _keycloakClientAuth;
    private readonly IUserRepository _userRepository;
    private readonly ITotpRepository _secretRepo;
    private readonly ITotpCache _totpCache;
    private readonly IAuthLockoutService _authLockout;
    private readonly IErrorCatalog _errors;

    public MfaService(
        ISmsVerificationService smsVerification,
        IEmailVerificationService emailVerification,
        IKeycloakClientUser keycloakClientUser,
        IKeycloakClientAuthentication keycloakClientAuth,
        IUserRepository userRepository,
        ITotpRepository secretRepo,
        ITotpCache cache,
        IAuthLockoutService authLockout,
        IErrorCatalog errors)
    {
        _smsVerification = smsVerification;
        _emailVerification = emailVerification;
        _keycloakClientUser = keycloakClientUser;
        _keycloakClientAuth = keycloakClientAuth;
        _userRepository = userRepository;
        _secretRepo = secretRepo;
        _totpCache = cache;
        _authLockout = authLockout;
        _errors = errors;
    }

    private static string MfaLockKey(Guid keycloakUserId) => $"mfa:{keycloakUserId}";

    private const int MaxMfaResends = 3;
    private static readonly TimeSpan MfaResendCooldown = TimeSpan.FromSeconds(60);
    // Mirrors CacheConstants.TotpCacheTtlMins (Infrastructure layer) — keep in sync.
    private static readonly TimeSpan PendingLoginAttemptWindow = TimeSpan.FromMinutes(10);

    /// Enforces the resend cap/cooldown for send-email and send-sms. Returns a
    /// failure result if the caller should be refused, otherwise null.
    private Result<bool>? CheckMfaResendAllowed(LoginAttemptCached attempt)
    {
        if (attempt.ResendCount >= MaxMfaResends)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.MfaResendLimitExceeded);
        }

        if (attempt.LastResendUtc is not null && DateTime.UtcNow - attempt.LastResendUtc.Value < MfaResendCooldown)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.MfaResendLimitExceeded);
        }

        return null;
    }

    /// Records a resend and re-stores the attempt without extending it past its
    /// original expiry, so repeated resends can't keep the pending attempt (and
    /// its cached tokens) alive indefinitely.
    private async Task RegisterMfaResendAsync(string setupToken, LoginAttemptCached attempt)
    {
        attempt.ResendCount++;
        attempt.LastResendUtc = DateTime.UtcNow;

        var remaining = PendingLoginAttemptWindow - (DateTime.UtcNow - attempt.CreatedAtUtc);
        if (remaining <= TimeSpan.Zero)
        {
            remaining = TimeSpan.FromSeconds(1);
        }

        await _totpCache.StoreLoginAttemptAsync(setupToken, attempt, remaining);
    }

    /// Registers an MFA verification failure. If the failure trips the lockout
    /// threshold, revokes the Keycloak session issued at the password step and
    /// deletes the pending login attempt, since the token already exists and
    /// hiding the cache entry alone would not invalidate it.
    private async Task<Result<LoginResponseDto>> RegisterMfaFailureAsync(
        string setupToken, LoginAttemptCached attempt, string errorCode)
    {
        var mfaKey = MfaLockKey(attempt.KeycloakUserId);
        await _authLockout.RegisterLoginFailureAsync(mfaKey);

        if (await _authLockout.IsLockedAsync(mfaKey))
        {
            await _keycloakClientAuth.LogoutAsync(attempt.RefreshToken);
            await _totpCache.RemoveLoginAttemptAsync(setupToken);
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.TooManyAttempts);
        }

        return _errors.Fail<LoginResponseDto>(errorCode);
    }

    /// Set/Change User's Mfa Type
    public async Task<Result<bool>> SetMfaTypeAsync(string username, string mfaType)
    {
        if (!Enum.TryParse<MfaType>(mfaType, true, out var parsedMfaType))
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.InvalidMfaType);
        }

        var dbUser = await _userRepository.GetByUsernameAsync(username);
        if (dbUser == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInDB);
        }

        dbUser.MfaType = parsedMfaType;
        await _userRepository.UpdateAsync(dbUser);

        return Result<bool>.Ok(data: true, message: "Mfa Set successfully.");
    }

    /// Generate TOTP QR Code and Secret 
    public async Task<Result<TotpSetupDto>> GenerateTotpCodeAsync(string username, string issuer = "Auth")
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

        return Result<TotpSetupDto>.Ok(data: topSetup, message: "Totp generated");
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

        return Result<bool>.Ok(data: true, message: "Totp registered");
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

        var mfaKey = MfaLockKey(loginAttempt.KeycloakUserId);
        if (await _authLockout.IsLockedAsync(mfaKey))
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.TooManyAttempts);
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
            return await RegisterMfaFailureAsync(setupToken, loginAttempt, ErrorCodes.AUTH.InvalidTOTPcode);
        }

        // 4. Clean up LoginAttempt cache
        await _totpCache.RemoveLoginAttemptAsync(setupToken);
        await _authLockout.RegisterLoginSuccessAsync(mfaKey);
        await _authLockout.RegisterLoginSuccessAsync(loginAttempt.LoginKey);

        // 5. Update the last verified timestamp for the TOTP secret
        secret.LastVerifiedAt = DateTime.UtcNow;
        await _secretRepo.UpdateAsync(secret);

        var loginResponse = new LoginResponseDto
        {
            MfaEnabled = true,
            MfaMethod = MfaType.Totp.ToString().ToLower(),
            AccessToken = loginAttempt.AccessToken,
            RefreshToken = loginAttempt.RefreshToken,
            ExpiresIn = loginAttempt.ExpiresIn,
            RememberMe = loginAttempt.RememberMe
        };

        return Result<LoginResponseDto>.Ok(loginResponse);
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
        await DisableMfaAsync(username);

        return Result<bool>.Ok(true, message: "TOTP disabled successfully");
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

        var resendCheck = CheckMfaResendAllowed(loginAttempt);
        if (resendCheck is not null)
        {
            return resendCheck;
        }

        var result = await _emailVerification.SendMfaCodeAsync(loginAttempt.Email!);
        if (!result.Success)
        {
            return _errors.Fail<bool>(result.ErrorCode!);
        }

        await RegisterMfaResendAsync(setupToken, loginAttempt);

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

        var mfaKey = MfaLockKey(loginAttempt.KeycloakUserId);
        if (await _authLockout.IsLockedAsync(mfaKey))
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.TooManyAttempts);
        }

        var result = await _emailVerification.VerifyMfaCodeAsync(loginAttempt.Email!, code);
        if (!result.Success)
        {
            return await RegisterMfaFailureAsync(setupToken, loginAttempt, result.ErrorCode!);
        }

        await _totpCache.RemoveLoginAttemptAsync(setupToken);
        await _authLockout.RegisterLoginSuccessAsync(mfaKey);
        await _authLockout.RegisterLoginSuccessAsync(loginAttempt.LoginKey);

        var loginResponse = new LoginResponseDto
        {
            MfaEnabled = true,
            MfaMethod = MfaType.Totp.ToString().ToLower(),
            AccessToken = loginAttempt.AccessToken,
            RefreshToken = loginAttempt.RefreshToken,
            ExpiresIn = loginAttempt.ExpiresIn,
            RememberMe = loginAttempt.RememberMe
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

        var resendCheck = CheckMfaResendAllowed(loginAttempt);
        if (resendCheck is not null)
        {
            return resendCheck;
        }

        var result = await _smsVerification.SendMfaSmsAsync(loginAttempt.PhoneNumber);
        if (!result.Success)
        {
            return _errors.Fail<bool>(result.ErrorCode!);
        }

        await RegisterMfaResendAsync(setupToken, loginAttempt);

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

        var mfaKey = MfaLockKey(loginAttempt.KeycloakUserId);
        if (await _authLockout.IsLockedAsync(mfaKey))
        {
            return _errors.Fail<LoginResponseDto>(ErrorCodes.AUTH.TooManyAttempts);
        }

        var isValid = _smsVerification.VerifyMfaCode(loginAttempt.PhoneNumber!, code);
        if (!isValid)
        {
            return await RegisterMfaFailureAsync(setupToken, loginAttempt, ErrorCodes.AUTH.InvalidSmsCode);
        }

        await _totpCache.RemoveLoginAttemptAsync(setupToken);
        await _authLockout.RegisterLoginSuccessAsync(mfaKey);
        await _authLockout.RegisterLoginSuccessAsync(loginAttempt.LoginKey);

        var loginResponse = new LoginResponseDto
        {
            MfaEnabled = true,
            MfaMethod = MfaType.Totp.ToString().ToLower(),
            AccessToken = loginAttempt.AccessToken,
            RefreshToken = loginAttempt.RefreshToken,
            ExpiresIn = loginAttempt.ExpiresIn,
            RememberMe = loginAttempt.RememberMe
        };

        return Result<LoginResponseDto>.Ok(loginResponse);
    }    

    private async Task<Result<bool>> DisableMfaAsync(string username)
    {
        var dbUser = await _userRepository.GetByUsernameAsync(username);
        if (dbUser == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInDB);
        }

        dbUser.MfaType = MfaType.None;
        await _userRepository.UpdateAsync(dbUser);

        return Result<bool>.Ok(data: true, message: "Mfa Set In DB");
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
