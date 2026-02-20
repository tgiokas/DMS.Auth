using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IEmailSender
{
    Task<bool> SendEmailAsync(NotificationEmailDto notification);
}