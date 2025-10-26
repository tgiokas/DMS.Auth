using Authentication.Application.Dtos;
using Authentication.Application.Errors;
using Authentication.Application.Interfaces;

namespace Authentication.Application.Services;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly IUserManagementService _userService;
    private readonly IKeycloakClientUser _keycloakClientUser;
    private readonly IEmailSender _emailSender;
    private readonly IEmailCache _emailCache;                            
    private readonly IErrorCatalog _errors;

    public EmailVerificationService(
        IUserManagementService userService,
        IKeycloakClientUser keycloakClientUser,
        IEmailSender emailSender,
        IEmailCache cache,        
        IErrorCatalog errors)
    {
        _userService = userService;
        _keycloakClientUser = keycloakClientUser;
        _emailSender = emailSender;
        _emailCache = cache;        
        _errors = errors;
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

        var verifyUrl = $"https://your-frontend.com/auth/verify-email?token={token}";       
        var subject = $"Verification email for {email}";
        var message = $"Click the link to verify your email: {verifyUrl}";
        
        try
        {
            var sent = await _emailSender.SendVerificationEmailAsync(email, subject, message);
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

        var result = await _userService.EmailVerifiedAsync(email);
        if (!result.Success)
        {
            return _errors.Fail<bool>(result.ErrorCode!);
        }

        await _emailCache.RemoveTokenAsync(token);

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

        try
        {
            var sent = await _emailSender.SendVerificationEmailAsync(email, subject, message);
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

        var result = await _userService.EmailVerifiedAsync(email);
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

        try
        {
            var sent = await _emailSender.SendVerificationEmailAsync(email, subject, message);
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
        var isValid = string.Equals(cachedCode, code, StringComparison.OrdinalIgnoreCase);

        if (!isValid)
         {
            return _errors.Fail<bool>(ErrorCodes.AUTH.InvalidEmailCode);
        }

        await _emailCache.RemoveCodeAsync(email);

        return Result<bool>.Ok(data: true, message: "Mfa Email verified.");
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
