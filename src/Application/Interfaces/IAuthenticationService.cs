using DMS.Auth.Application.Dtos;

namespace DMS.Auth.Application.Interfaces;

public interface IAuthenticationService
{
    Task<TokenDto?> AuthenticateUserAsync(string username, string password);      
    Task<TokenDto?> RefreshTokenAsync(string refreshToken);
    MfaSecretDto GenerateMfaAuthCode(string username, string issuer = "DMS Auth");
    Task<bool> VerifyAndRegisterTotpAsync(string username, string code);
    Task<bool> LogoutAsync(string refreshToken);
    Task<string?> LoginWithGsis();
    Task<TokenDto?> GsisCallback(string code);
}
