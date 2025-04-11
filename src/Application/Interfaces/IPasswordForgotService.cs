namespace Authentication.Application.Interfaces;

public interface IPasswordForgotService
{
    Task SendResetLinkAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
}
