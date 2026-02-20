using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IUserManagementService
{
    Task<Result<Dtos.PagedResult<UserProfileDto>>> GetUsersAsync(UserQueryParams queryParams);
    Task<Result<UserProfileDto>> GetUserByNameAsync(string username);
    Task<Result<UserProfileDto>> GetUserByIdAsync(string userId);
    Task<Result<List<UserProfileDto>>> GetUsersByIdsAsync(List<IdDto> userIds);
    Task<Result<UserProfileDto>> CreateUserAsync(UserCreateDto request);
    Task<Result<UserProfileDto>> CreateUserWithRolesAsync(UserCreateDto request, List<RoleDto> rolesToAssign);
    Task<Result<UserProfileDto>> CreateUserAndSendEmailAsync(UserCreateDto request);    
    Task<Result<UserProfileDto>> UpdateUserAsync(UserUpdateDto request);
    Task<Result<bool>> DeleteUsersAsync(List<IdDto> userIds);
    Task<Result<bool>> FakeDeleteUsersAsync(List<IdDto> userIds);
    Task<Result<bool>> EnableUsersAsync(List<IdDto> userIds);
    Task<Result<bool>> DisableUsersAsync(List<IdDto> userIds);    
    Task<Result<IDictionary<string, string[]>>> GetUserAttributesAsync(string username);
    Task<Result<IDictionary<string, IDictionary<string, string[]>>>> GetUsersAttributesAsync(List<UserIdDto> userIds);
    Task<Result<bool>> SetUserAttributeAsync(string username, string key, string value);
    Task<Result<bool>> DeleteUserAttributeAsync(string username, string key);
}

