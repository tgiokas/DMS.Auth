using Authentication.Application.Dtos;
using Authentication.Domain.Enums;

namespace Authentication.Application.Interfaces;

public interface IPasswordResetService
{
    Task<Result<bool>> SendResetLinkAsync(string email, EmailTemplateType type);
    Task<Result<bool>> ResetPasswordAsync(string token, string newPassword);    
}
