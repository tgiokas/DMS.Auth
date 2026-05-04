using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IKeycloakClientAuthentication
{   
    Task<TokenDto?> GetUserAccessTokenAsync(string username, string password, bool offlineAccess = false);
    Task<TokenDto?> GetAccessTokenByCodeAsync(string code);
    Task<TokenDto?> RefreshTokenAsync(string refreshToken);    
    Task<bool> LogoutAsync(string refreshToken);
    Task<string> EntraLoginUrlAsync();
}