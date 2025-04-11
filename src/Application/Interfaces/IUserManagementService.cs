using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IUserManagementService
{
    Task<List<KeycloakUser>?> GetUsersAsync();
    Task<KeycloakUser?> GetUserProfile(string username);
    Task<bool> CreateUserAsync(UserCreateDto request);
    Task<bool> UpdateUserAsync(UserUpdateDto request);
    Task<bool> DeleteUserAsync(string username);
    Task<List<KeycloakRole>?> GetUserRolesAsync(string username);
    Task<bool> AssignRoleAsync(string username, string roleId);
    Task<bool> EnableMfaAsync(string username);
    Task MarkPhoneAsVerifiedAsync(string phoneNumber);
    Task MarkEmailAsVerifiedAsync(string email);
}