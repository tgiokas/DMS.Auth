using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IKeycloakClientUser
{     
    Task<List<KeycloakUser>?> GetUsersAsync();
    Task<string?> GetUserIdByUsernameAsync(string username);    
    Task<KeycloakUser?> GetUserByNameAsync(string username);
    Task<KeycloakUser?> GetUserByIdAsync(string userId);
    Task<KeycloakUser?> GetUserByEmailAsync(string email);
    Task<KeycloakUser?> CreateUserAsync(KeycloakUserDto userCreateDto);
    Task<Result<bool>> UpdateUserAsync(KeycloakUserDto userUpdateDto);
    Task<bool> UpdateUserPasswordAsync(string userId, string newPassword, bool temporary);
    Task<bool> DeleteUserAsync(string username);
    Task<IDictionary<string, string[]>> GetUserAttributesAsync(string userId);
    Task<bool> SetUserAttributeAsync(string userId, string key, string value);
    Task<bool> UpdateUserAttributesAsync(string userId, IDictionary<string, string[]> attributes);
}