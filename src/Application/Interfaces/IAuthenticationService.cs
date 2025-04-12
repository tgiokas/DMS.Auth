using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IAuthenticationService
{
    Task<LoginResult?> LoginUserAsync(string username, string password);   
    Task<TokenDto?> RefreshTokenAsync(string refreshToken);
    Task<bool> LogoutAsync(string refreshToken);
    Task<string?> LoginWithGsis();
    Task<TokenDto?> GsisCallback(string code);
}
