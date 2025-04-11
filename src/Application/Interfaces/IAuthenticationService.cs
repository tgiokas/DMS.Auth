using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IAuthenticationService
{
    Task<LoginResult?> LoginUserAsync(string username, string password);
    TotpSetupDto GenerateTotpCode(string username, string issuer = "Auth");
    Task<bool> RegisterTotpAsync(string username, string code, string setupToken);
    Task<LoginResult> VerifyLoginTotpAsync(string setupToken, string code);
    Task<TokenDto?> RefreshTokenAsync(string refreshToken);
    Task<bool> LogoutAsync(string refreshToken);
    Task<string?> LoginWithGsis();
    Task<TokenDto?> GsisCallback(string code);
}
