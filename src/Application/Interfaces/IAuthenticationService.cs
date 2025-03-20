using DMS.Auth.Application.Dtos;

namespace DMS.Auth.Application.Interfaces;

public interface IAuthenticationService
{
    Task<TokenDto?> AuthenticateUserAsync(string username, string password);      
    Task<TokenDto?> RefreshTokenAsync(string refreshToken);
    Task<TokenTempDto?> GetTempTokenAsync(string username, string password);
    Task<MfaEnrollmentResponse?> GetMfaAuthCode(string tempToken);
    Task<TokenDto?> VerifyMfa(MfaVerificationRequest request);
    Task<bool> LogoutAsync(string refreshToken);

    Task<string?> LoginWithGsis();
    Task<TokenDto?> GsisCallback(string code);
}
