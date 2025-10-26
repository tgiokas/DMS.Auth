using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface ISmsVerificationService
{
    Task<Result<bool>> SendVerificationSmsAsync(string phoneNumber); // send Sms with OTP Code
    Task<Result<bool>> VerifySmsAsync(string phoneNumber, string code);  // for verification at signup 
    Task<Result<bool>> SendMfaSmsAsync(string phoneNumber); // send MFA sms
    public bool VerifyMfaCode(string phoneNumber, string code);  // for MFA login
}
