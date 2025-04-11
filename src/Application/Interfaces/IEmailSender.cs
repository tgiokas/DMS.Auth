namespace Authentication.Application.Interfaces;

public interface IEmailSender
{
    Task<bool> SendVerificationEmailAsync(string recipient, string subject, string message);
}

