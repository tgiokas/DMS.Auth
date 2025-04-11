namespace Authentication.Application.Interfaces;

public interface IEmailVerificationService
{
    Task SendVerificationEmailAsync(string email);
    Task<bool> VerifyEmailAsync(string token);
}
