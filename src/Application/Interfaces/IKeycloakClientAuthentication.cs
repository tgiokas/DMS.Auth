using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IKeycloakClientAuthentication
{   
    Task<TokenDto?> GetUserAccessTokenAsync(string username, string password);
    Task<TokenDto?> GetAccessTokenByCodeAsync(string code);
    Task<TokenDto?> RefreshTokenAsync(string refreshToken);    
    Task<bool> LogoutAsync(string refreshToken);
}