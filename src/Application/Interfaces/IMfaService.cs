using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IMfaService
{
    Task<Result<TotpSetupDto>> GenerateTotpCode(string username, string issuer = "Auth");
    Task<Result<bool>> RegisterTotpAsync(string username, string code, string setupToken);
    Task<Result<bool>> DisableTotpAsync(string username);
    Task<Result<LoginResponseDto>> VerifyLoginByTotpAsync(string setupToken, string code);
    Task<Result<bool>> SendEmailCodeAsync(string setupToken);
    Task<Result<LoginResponseDto>> VerifyLoginByEmailAsync(string setupToken, string code);
    Task<Result<bool>> SendSmsCodeAsync(string setupToken);
    Task<Result<LoginResponseDto>> VerifyLoginBySmsAsync(string setupToken, string code);
}
