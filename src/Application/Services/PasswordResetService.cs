using Microsoft.Extensions.Options;
 
using Authentication.Application.Configuration;
using Authentication.Application.Dtos;
using Authentication.Application.Errors;
using Authentication.Application.Interfaces;
using Authentication.Domain.Enums;

namespace Authentication.Application.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly IKeycloakClientUser _keycloakClientUser;
    private readonly IEmailSender _emailSender;    
    private readonly IPasswordResetCache _passwordResetCache;
    private readonly IErrorCatalog _errors;
    private readonly string _passwordResetUrl;

    public PasswordResetService(
        IKeycloakClientUser keycloakClient,
        IEmailSender emailSender,
        IPasswordResetCache cache,
        IOptions<AuthSettings> authOptions,
        IErrorCatalog errors)
    {
        _keycloakClientUser = keycloakClient;
        _emailSender = emailSender;
        _passwordResetCache = cache;
        _errors = errors;
        _passwordResetUrl = authOptions.Value.PasswordResetUrl;
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

        var resetUrl = $"{_passwordResetUrl}?token={token}";
        var subject = $"Reset Password for {email}";
        var message = $"Click the link to reset your password: {resetUrl}";       

        var emailMessageDto = new NotificationEmailDto
        {
            Recipient = email,
            Subject = subject,
            Message = message,
            Type = EmailTemplateType.PasswordReset,            
            TemplateParams = new Dictionary<string, string>
            {
                ["Username"] = email,
                ["PasswordResetLink"] = resetUrl
            }
        };

        try
        {
            var sent = await _emailSender.SendEmailAsync(emailMessageDto);
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

        var updateDto = new KeycloakUserDto
        {
            Id = cachedEntry.UserId,
            EmailVerified = true            
        };

        var keycloakUpdateResult = await _keycloakClientUser.UpdateUserAsync(updateDto);
        if (!keycloakUpdateResult.Success)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UpdateInKeycloakFailed);
        }        

        return Result<bool>.Ok(data: true, message: "Email verified & Password reset.");
    }
}
