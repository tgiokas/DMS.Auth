using Microsoft.Extensions.Logging;
using Authentication.Application.Interfaces;

namespace Authentication.Application.Services;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailCacheService _cache;
    private readonly IUserManagementService _userService;    
    private readonly ILogger<EmailVerificationService> _logger;

    public EmailVerificationService(IEmailSender emailSender,
        IEmailCacheService cache,
        IUserManagementService userService,        
        ILogger<EmailVerificationService> logger)
    {
        _emailSender = emailSender;
        _cache = cache;
        _userService = userService;        
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string email)
    {
        var token = GenerateToken();

        _cache.StoreToken(token, email, TimeSpan.FromMinutes(15));

        var verifyUrl = $"https://your-frontend.com/auth/verify-email?token={token}";
        var message = $"Click the link to verify your email: {verifyUrl}";
        var subject = $"Verification email for {email}";

        await _emailSender.SendVerificationEmailAsync(email, subject, message);

        _logger.LogInformation("Sent email verification to {Email}", email);
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var email = _cache.GetEmailByToken(token);
        if (string.IsNullOrWhiteSpace(email))
            return false;

        _cache.RemoveToken(token);

        await _userService.MarkEmailAsVerifiedAsync(email);
        return true;
    }

    private string GenerateToken()
    {        
        return Guid.NewGuid().ToString("N");
    }
}
