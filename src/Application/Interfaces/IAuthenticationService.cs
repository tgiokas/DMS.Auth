using DMS.Auth.Application.Dtos;

namespace DMS.Auth.Application.Interfaces;

public interface IAuthenticationService
{
    Task<string?> AuthenticateUserAsync(string username, string password);
    Task<TokenDto?> RefreshTokenAsync(string refreshToken);
    Task<bool> LogoutAsync(string refreshToken);
}
