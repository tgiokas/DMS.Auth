using Microsoft.Extensions.Logging;

using Authentication.Application.Interfaces;
using Authentication.Application.Dtos;

namespace Authentication.Application.Services;

public class PasswordForgotService : IPasswordForgotService
{
    private readonly IPasswordForgotCacheService _cache;
    private readonly IKeycloakClient _keycloakClient;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<PasswordForgotService> _logger;

    public PasswordForgotService(
        IPasswordForgotCacheService cache,
        IKeycloakClient keycloakClient,
        IEmailSender emailSender,
        ILogger<PasswordForgotService> logger)
    {
        _cache = cache;
        _keycloakClient = keycloakClient;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendResetLinkAsync(string email)
    {        
        var user = await _keycloakClient.GetUserByEmailAsync(email);
        if (user == null || user.Id == null)
        {
            _logger.LogWarning("No user found for email {Email}", email);
            return;
        }

        var token = Guid.NewGuid().ToString("N");

        _cache.StoreToken(token, new PasswordForgotTokenEntry
        {
            Email = email,
            UserId = user.Id
        });

        var resetUrl = $"https://yourapp.com/auth/reset-password?token={token}";
        var message = $"Click the link to reset your password: {resetUrl}";
        var subject = $"Reset Password for  {email}";

        await _emailSender.SendVerificationEmailAsync(email, subject, message);
        _logger.LogInformation("Password reset link sent to {Email}", email);
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var entry = _cache.GetToken(token);
        if (entry == null)
            return false;

        await _keycloakClient.UpdateUserPasswordAsync(entry.UserId, newPassword);

        _cache.RemoveToken(token);
        return true;
    }
}
