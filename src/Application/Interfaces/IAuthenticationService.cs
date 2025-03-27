using DMS.Auth.Application.Dtos;

namespace DMS.Auth.Application.Interfaces;

public interface IAuthenticationService
{
    Task<LoginResult?> LoginUserAsync(string username, string password);      
    Task<TokenDto?> RefreshTokenAsync(string refreshToken);
    TotpSetupDto GenerateTotpCode(string username, string issuer = "DMS Auth");
    Task<bool> RegisterTotpAsync(string username, string code, string setupToken);
    Task<LoginResult> VerifyLoginTotpAsync(string setupToken, string code);
    Task<bool> LogoutAsync(string refreshToken);
    Task<string?> LoginWithGsis();
    Task<TokenDto?> GsisCallback(string code);
}
