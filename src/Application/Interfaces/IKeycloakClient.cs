using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IKeycloakClient
{   
    Task<TokenDto?> GetUserAccessTokenAsync(string username, string password);
    Task<TokenDto?> RefreshTokenAsync(string refreshToken);    
    Task<bool> LogoutAsync(string refreshToken);
    Task<string> GsisLoginUrl();
    Task<TokenDto?> GsisCallback(string code);

    Task<List<KeycloakUser>?> GetUsersAsync();
    Task<string?> GetUserIdByUsernameAsync(string username);    
    Task<KeycloakUser?> GetUserProfileAsync(string username);
    Task<KeycloakUser?> GetUserByEmailAsync(string email);
    Task<bool> CreateUserAsync(string username, string email, string password);
    Task<bool> UpdateUserAsync(UserUpdateDto request);
    Task UpdateUserPasswordAsync(string userId, string newPassword);
    Task<bool> DeleteUserAsync(string username);

    Task<List<KeycloakRole>?> GetUserRolesAsync(string username);
    Task<bool> CreateRoleAsync(string roleName, string roleDescr, string realm);
    Task<bool> AssignRoleAsync(string username, string roleId);
}