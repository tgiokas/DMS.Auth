namespace Authentication.Application.Interfaces;

public interface IEmailSender
{
    Task<bool> SendVerificationEmailAsync(string email, string subject, string message);
}