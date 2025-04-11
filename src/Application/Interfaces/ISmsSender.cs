namespace Authentication.Application.Interfaces;

public interface ISmsSender
{
    Task<bool> SendVerificationSmsAsync(string phoneNumber, string message);
}