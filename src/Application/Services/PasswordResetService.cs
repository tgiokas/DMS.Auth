using Authentication.Application.Dtos;
using Authentication.Application.Errors;
using Authentication.Application.Interfaces;
using Authentication.Domain.Interfaces;

namespace Authentication.Application.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly IKeycloakClientUser _keycloakClientUser;
    private readonly IEmailSender _emailSender;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetCache _passwordResetCache;
    private readonly IErrorCatalog _errors;

    public PasswordResetService(
        IKeycloakClientUser keycloakClient,       
        IEmailSender emailSender,
        IUserRepository userRepository,
        IPasswordResetCache cache,
        IErrorCatalog errors)
    {
        _keycloakClientUser = keycloakClient;        
        _emailSender = emailSender;
        _userRepository = userRepository;
        _passwordResetCache = cache;
        _errors = errors;
    }

    public async Task<Result<bool>> SendResetLinkAsync(string email)
    {        
        var user = await _keycloakClientUser.GetUserByEmailAsync(email);
        if (user == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        var token = Guid.NewGuid().ToString("N");
        await _passwordResetCache.StoreTokenAsync(token, new PasswordResetCached
        {
            Email = email,
            UserId = user.Id
        });

        var resetUrl = $"https://yourapp.com/auth/reset-password?token={token}";
        var message = $"Click the link to reset your password: {resetUrl}";
        var subject = $"Reset Password for {email}";

        try
        {
            var sent = await _emailSender.SendVerificationEmailAsync(email, subject, message);
            if (sent)
            {
                return Result<bool>.Ok(data: true, message: "Email for password reset sent.");
            }
            else
            {
                return _errors.Fail<bool>(ErrorCodes.AUTH.EmailVerificationSendFailed);
            }
        }
        catch (Exception)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.EmailVerificationSendFailed);
        }
    }

    public async Task<Result<bool>> ResetPasswordAsync(string token, string newPassword)
    {
        var cachedEntry = await _passwordResetCache.GetTokenAsync(token);
        if (cachedEntry == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.PasswordResetTokenInvalid);
        }

        var result = await _keycloakClientUser.UpdateUserPasswordAsync(cachedEntry.UserId, newPassword, false);
        if (!result)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UpdatePasswordFailed);
        }

        await _passwordResetCache.RemoveTokenAsync(token);

        return Result<bool>.Ok(data: true, message: "Password reset successful.");
    }

    public async Task<Result<bool>> ResetPasswordAndVerifyEmailAsync(string token, string newPassword)
    {
        var cachedEntry = await _passwordResetCache.GetTokenAsync(token);
        if (cachedEntry == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.PasswordResetTokenInvalid);
        }
        
        var result = await _keycloakClientUser.UpdateUserPasswordAsync(cachedEntry.UserId, newPassword, false);
        if (!result)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UpdatePasswordFailed);
        }

        await _passwordResetCache.RemoveTokenAsync(token);

        var updateDto = new KeycloakUserDto
        {
            Id = cachedEntry.UserId,
            EmailVerified = true,
            Enabled = true
        };

        var keycloakUpdateResult = await _keycloakClientUser.UpdateUserAsync(updateDto);
        if (!keycloakUpdateResult.Success)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UpdateInKeycloakFailed);
        }

        var dbUser = await _userRepository.GetByEmailAsync(cachedEntry.Email);
        if (dbUser == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInDB);
        }

        dbUser.EmailVerified = true;
        dbUser.IsEnabled = true;
        await _userRepository.UpdateAsync(dbUser);        

        return Result<bool>.Ok(data: true, message: "Email verified & Password reset.");
    }
}
