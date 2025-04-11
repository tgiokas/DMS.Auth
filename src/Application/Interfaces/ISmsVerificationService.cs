namespace Authentication.Application.Interfaces;

public interface ISmsVerificationService
{
    Task SendVerificationSmsAsync(string phoneNumber);
    Task<bool> VerifySmsAsync(string phoneNumber, string code);
}
