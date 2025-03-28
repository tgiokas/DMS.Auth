using DMS.Auth.Application.Dtos;

namespace DMS.Auth.Application.Interfaces;

public interface IKeycloakClient
{   
    Task<TokenDto?> GetUserAccessTokenAsync(string username, string password);
    Task<TokenDto?> GetAdminAccessTokenAsync();
    Task<TokenDto?> RefreshTokenAsync(string refreshToken);    
    Task<bool> LogoutAsync(string refreshToken);
    Task<string> GsisLoginUrl();
    Task<TokenDto?> GsisCallback(string code);

    Task<List<KeycloakUserDto>> GetUsersAsync();
    Task<string?> GetUserIdByUsernameAsync(string username);
    Task<Credential?> GetUserCredentialsAsync(string userId);
    Task<bool> CreateUserAsync(string username, string email, string password);
    Task<bool> UpdateUserAsync(UserUpdateDto request);
    Task<bool> DeleteUserAsync(string username);

    Task<List<KeycloakRoleDto>> GetUserRolesAsync(string username);
    Task<bool> CreateRoleAsync(string roleName, string roleDescr, string realm);
    Task<bool> AssignRoleAsync(string username, string roleId);
}