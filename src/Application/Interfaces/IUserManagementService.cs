using Authentication.Application.Dtos;
using Authentication.Domain.Enums;

namespace Authentication.Application.Interfaces;

public interface IUserManagementService
{
    Task<Result<Dtos.PagedResult<UserProfileDto>>> GetUsersAsync(UserQueryParams queryParams);
    Task<Result<UserProfileDto>> GetUserProfileByName(string username);
    Task<Result<UserProfileDto>> GetUserProfileById(string userId);
    Task<Result<List<UserProfileDto>>> GetUserProfilesByIds(List<IdDto> userIds);
    Task<Result<UserProfileDto>> CreateUserAsync(UserCreateDto request);
    Task<Result<UserProfileDto>> CreateUserWithRolesAsync(UserCreateDto request, List<RoleDto> rolesToAssign);
    Task<Result<UserProfileDto>> CreateUserAndSendEmailAsync(UserCreateDto request);    
    Task<Result<UserProfileDto>> UpdateUserAsync(UserUpdateDto request);
    Task<Result<bool>> DeleteUserAsync(string userId);
    Task<Result<IDictionary<string, string[]>>> GetUserAttributesAsync(string username);
    Task<Result<IDictionary<string, IDictionary<string, string[]>>>> GetUsersAttributesAsync(List<string> usernames);
    Task<Result<bool>> SetUserAttributeAsync(string username, string key, string value);
    Task<Result<bool>> DeleteUserAttributeAsync(string username, string key);
    Task<Result<bool>> ResetPasswordAndVerifyEmailAsync(PasswordResetDto request);
    Task<Result<bool>> EnableMfaAsync(string username, MfaType mfaType);
    Task<Result<bool>> DisableMfaAsync(string username);
    Task<Result<bool>> EmailVerifiedAsync(string email);
    Task<Result<bool>> PhoneVerifiedAsync(string phoneNumber);
}

