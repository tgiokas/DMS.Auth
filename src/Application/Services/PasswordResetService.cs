using Microsoft.Extensions.Options;
 
using Authentication.Application.Configuration;
using Authentication.Application.Dtos;
using Authentication.Application.Errors;
using Authentication.Application.Extensions;
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
    private readonly string _passwordSetUrl;

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
        _passwordSetUrl = authOptions.Value.PasswordSetUrl;
    }

    public async Task<Result<bool>> SendResetLinkAsync(string email, EmailTemplateType type)
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
            UserId = user.Id,
            Username = user.UserName
        });

        var setUrl = $"{_passwordSetUrl}?token={token}";
        var resetUrl = $"{_passwordResetUrl}?token={token}";

        string subject;
        string message;
        var templateParams = new Dictionary<string, string>
        {
            ["Username"] = user.UserName,           
        };        

        if (type == EmailTemplateType.InitialPasswordSet)
        {
            subject = "Επιτυχής Εγγραφή στο Archium || Successful Registration on Archium";
            message = $"Click the link to Set your password: {resetUrl}";

            templateParams["Firstname"] = user.FirstName ?? string.Empty;
            templateParams["Lastname"] = user.LastName ?? string.Empty;
            templateParams["PasswordSetLink"] = setUrl;
        }
        else if (type == EmailTemplateType.PasswordReset)
        {
            subject = "Αλλαγή / Επαναφορά Κωδικού Πρόσβασης || Password Change / Reset";
            message = $"Click the link to reset your password: {resetUrl}";
            templateParams["PasswordResetLink"] = resetUrl;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(type), $"Unsupported email type: {type}");
        }

        var emailMessageDto = new NotificationEmailDto
        {
            Recipient = email,
            Subject = subject,
            Message = message,
            Type = type,
            TemplateParams = templateParams
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

        var subject = $"Επιτυχής Αλλαγή Κωδικού Πρόσβασης || Successful Password Change";
        var message = $"Your Archium account password was successfully changed";

        var emailMessageDto = new NotificationEmailDto
        {
            Recipient = cachedEntry.Email,
            Subject = subject,
            Message = message,
            Type = EmailTemplateType.PasswordResetSuccess,
            TemplateParams = new Dictionary<string, string>
            {
                ["Username"] = cachedEntry.Username,
                ["ChangedAt"] = DateTime.UtcNow.ToGreekDateTime().ToString()
            }
        };

        try
        {
            var sent = await _emailSender.SendEmailAsync(emailMessageDto);
            if (sent)
            {
                return Result<bool>.Ok(data: true, message: "Email verified & Password reset.");
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
}
