using DMS.Auth.Application.Dtos;

namespace DMS.Auth.Application.Interfaces;

public interface IAuthenticationService
{
    Task<TokenResponse> AuthenticateUserAsync(string username, string password);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken);
    Task<bool> LogoutAsync(string refreshToken);
}
