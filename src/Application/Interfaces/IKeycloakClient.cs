using DMS.Auth.Application.Dtos;

namespace DMS.Auth.Application.Interfaces;

public interface IKeycloakClient
{
    Task<List<KeycloakUser>> GetUsersAsync();
    Task<string> GetUserIdByUsernameAsync(string username);
    Task<TokenResponse> GetTokenAsync(string username, string password);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken);    
    Task<bool> CreateUserAsync(string username, string email, string password);
    Task<bool> UpdateUserAsync(UpdateUserRequest request);
    Task<List<KeycloakRole>> GetUserRolesAsync(string username);
    Task<bool> AssignRoleAsync(string username, string roleId);
    Task<bool> EnableMfaAsync(string username);
    Task<bool> LogoutAsync(string refreshToken);    
    Task<bool> DeleteUserAsync(string username);    
}