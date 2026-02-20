using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Authentication.Application.Dtos;
using Authentication.Application.Errors;
using Authentication.Application.Interfaces;
using Authentication.Domain.Enums;

namespace Authentication.Application.Services;

public class EmailVerificationService : IEmailVerificationService
{   
    private readonly IKeycloakClientUser _keycloakClientUser;
    private readonly IEmailSender _emailSender;
    private readonly IEmailCache _emailCache;
    private readonly IConfiguration _configuration;
    private readonly IErrorCatalog _errors;
    private readonly ILogger<EmailVerificationService> _logger;
    private readonly string _verificationUrl;

    public EmailVerificationService(       
        IKeycloakClientUser keycloakClientUser,
        IEmailSender emailSender,
        IEmailCache cache,
        IConfiguration configuration,
        IErrorCatalog errors,
        ILogger<EmailVerificationService> logger
        )
    {        
        _keycloakClientUser = keycloakClientUser;
        _emailSender = emailSender;
        _emailCache = cache;
        _configuration = configuration;
        _errors = errors;
        _logger = logger;

        _verificationUrl = _configuration["VERIFICATION_URL"] ?? throw new ArgumentNullException(nameof(configuration), "VERIFICATION_URL is empty.");
    }

    public async Task<Result<bool>> SendVerificationLinkAsync(string email)
    {
        var user = await _keycloakClientUser.GetUserByEmailAsync(email);
        if (user == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        var token = GenerateToken();
        await _emailCache.StoreTokenAsync(token, email);

        var verifyUrl = $"{_verificationUrl}?token={token}";
        var subject = $"Verification email for {email}";
        var message = $"Click the link to verify your email: {verifyUrl}";       

        var emailMessageDto = new NotificationEmailDto
        {
            Recipient = email,
            Subject = subject,
            Message = message,
            Type = EmailTemplateType.VerificationLink,
            TemplateParams = new Dictionary<string, string>
            {
                ["Username"] = email,
                ["VerificationLink"] = verifyUrl
            }
        };

        try
        {
            var sent = await _emailSender.SendEmailAsync(emailMessageDto);
            if (sent)
            {                
                return Result<bool>.Ok(data: true, message: "Email verification sent.");
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

    public async Task<Result<bool>> VerifyEmailLinkAsync(string token)
    {
        var email = await _emailCache.GetEmailByTokenAsync(token);
        if (string.IsNullOrWhiteSpace(email))
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.VerifyEmailTokenInValid);
        }

        await _emailCache.RemoveTokenAsync(token);

        var result = await EmailVerifiedAsync(email);
        if (!result.Success)
        {
            return _errors.Fail<bool>(result.ErrorCode!);
        }      

        return Result<bool>.Ok(data:true, message: "Email verified.");
    }

    public async Task<Result<bool>> SendVerificationCodeAsync(string email)
    {
        var user = await _keycloakClientUser.GetUserByEmailAsync(email);
        if (user == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        var code = GenerateCode();
        await _emailCache.StoreCodeAsync(email, code);

        var subject = $"Email Verification Code for {email}";
        var message = $"Your verification code is: {code}";

        var emailMessageDto = new NotificationEmailDto
        {
            Recipient = email,
            Subject = subject,
            Message = message,
            Type = EmailTemplateType.VerificationCode,
            TemplateParams = new Dictionary<string, string>
            {
                ["Username"] = email,
                ["VerificationCode"] = code
            },           
        };

        try
        {
            var sent = await _emailSender.SendEmailAsync(emailMessageDto);
            if (sent)
            {
                return Result<bool>.Ok(true, "Verification code sent successfully.");
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

    public async Task<Result<bool>> VerifyEmailCodeAsync(string email, string code)
    {
        var cachedCode = await _emailCache.GetCodeAsync(email);
        if (string.IsNullOrWhiteSpace(cachedCode))
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.VerifyEmailTokenInValid);
        }

        var isValid = string.Equals(cachedCode, code, StringComparison.OrdinalIgnoreCase);
        if (!isValid)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.InvalidEmailCode);
        }

        await _emailCache.RemoveCodeAsync(email);

        var result = await EmailVerifiedAsync(email);
        if (!result.Success)
        {
            return _errors.Fail<bool>(result.ErrorCode!);
        }

        return Result<bool>.Ok(true, "Email verified successfully.");
    }
    
    public async Task<Result<bool>> SendMfaCodeAsync(string email)
    {
        var user = await _keycloakClientUser.GetUserByEmailAsync(email);
        if (user == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        var code = GenerateCode();
        await _emailCache.StoreCodeAsync(email, code);
       
        var subject = $"MFA email for {email}";
        var message = $"Your mfa code is: {code}";

        var emailMessageDto = new NotificationEmailDto
        {
            Recipient = email,
            Subject = subject,
            Message = message,
            Type = EmailTemplateType.MfaCode,
            TemplateParams = new Dictionary<string, string>
            {
                ["Username"] = email,
                ["MfaCode"] = code
            },
        };

        try
        {
            var sent = await _emailSender.SendEmailAsync(emailMessageDto);
            if (sent)
            {               
                return Result<bool>.Ok(data: true, message: "Mfa email sent");
            }
            else
            {                            
                return _errors.Fail<bool>(ErrorCodes.AUTH.EmailMfaSendFailed);
            }
        }
        catch (Exception)
        {            
            return _errors.Fail<bool>(ErrorCodes.AUTH.EmailMfaSendFailed);
        }
    }

    public async Task<Result<bool>> VerifyMfaCodeAsync(string email, string code)
    {
        var cachedCode = await _emailCache.GetCodeAsync(email);
        if (string.IsNullOrWhiteSpace(cachedCode))
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.VerifyEmailTokenInValid);
        }
        var isValid = string.Equals(cachedCode, code, StringComparison.OrdinalIgnoreCase);

        _logger.LogInformation("Is code Valid: {isValid}", isValid);

        if (!isValid)
        {            
            return _errors.Fail<bool>(ErrorCodes.AUTH.InvalidEmailCode);
        }        

        await _emailCache.RemoveCodeAsync(email);

        return Result<bool>.Ok(data: true, message: "Mfa Email verified.");
    }

    private async Task<Result<bool>> EmailVerifiedAsync(string email)
    {
        var keycloakUser = await _keycloakClientUser.GetUserByEmailAsync(email);
        if (keycloakUser == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        var updateDto = new KeycloakUserDto
        {
            Id = keycloakUser.Id,
            EmailVerified = true,
        };

        var keycloakUpdateResult = await _keycloakClientUser.UpdateUserAsync(updateDto);
        if (!keycloakUpdateResult.Success)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UpdateInKeycloakFailed);
        }       

        return Result<bool>.Ok(data: true, message: "Email Verified.");
    }

    private static string GenerateToken()
    {        
        return Guid.NewGuid().ToString("N");
    }

    private static string GenerateCode()
    {
        var random = new Random();
        return random.Next(100_000, 999_999).ToString();
    }
}
