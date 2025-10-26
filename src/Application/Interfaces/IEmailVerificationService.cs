using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IEmailVerificationService
{
    Task<Result<bool>> SendVerificationLinkAsync(string email); // send email with verification link    
    Task<Result<bool>> VerifyEmailLinkAsync(string token); // verify link at signup 
    Task<Result<bool>> SendVerificationCodeAsync(string email); // send email with 6-digit code
    Task<Result<bool>> VerifyEmailCodeAsync(string email, string code); // verify 6-digit code at signup
    Task<Result<bool>> SendMfaCodeAsync(string email); // send MFA code login code
    Task<Result<bool>> VerifyMfaCodeAsync(string email, string code); // verify MFA login code
}