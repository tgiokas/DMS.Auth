using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IPasswordResetService
{
    Task<Result<bool>> SendResetLinkAsync(string email);
    Task<Result<bool>> ResetPasswordAsync(string token, string newPassword);
    Task<Result<bool>> ResetPasswordAndVerifyEmailAsync(string token, string newPassword);
}
