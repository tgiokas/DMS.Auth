using System.Linq.Dynamic.Core;
using Microsoft.Extensions.Configuration;

using Authentication.Application.Dtos;
using Authentication.Application.Errors;
using Authentication.Application.Interfaces;
using Authentication.Domain.Entities;
using Authentication.Domain.Enums;
using Authentication.Domain.Interfaces;

namespace Authentication.Application.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IRoleManagementService _roleManagementService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly IKeycloakClientUser _keycloakClientUser;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly IErrorCatalog _errors;
    private readonly string AdminRoleName = "admin";

    public UserManagementService(         
        IRoleManagementService roleManagementService,
        IPasswordResetService passwordResetService,
        IKeycloakClientUser keycloakClientUser,
        IUserRepository userRepository,
        IConfiguration configuration,
        IErrorCatalog errors)
    {
        _roleManagementService = roleManagementService;
        _passwordResetService = passwordResetService;
        _keycloakClientUser = keycloakClientUser;
        _userRepository = userRepository;
        _configuration = configuration;
        _errors = errors;
    }

    public async Task<Result<Dtos.PagedResult<UserProfileDto>>> GetUsersAsync(UserQueryParams queryParams)
    {
        var keycloakUsers = await _keycloakClientUser.GetUsersAsync();
        if (keycloakUsers == null)
        {
            return _errors.Fail<Dtos.PagedResult<UserProfileDto>>(ErrorCodes.AUTH.UsersNotFound);
        }            

        // Merge Keycloak and auth-db user data        
        var localUsers = await _userRepository.GetAllAsync();
        var localUserDict = localUsers.ToDictionary(u => u.Username, StringComparer.OrdinalIgnoreCase);

        var userProfiles = new List<UserProfileDto>();
        foreach (var keycloakUser in keycloakUsers)
        {
            localUserDict.TryGetValue(keycloakUser.UserName ?? string.Empty, out var localUser);

            userProfiles.Add(MapToUserProfile(keycloakUser, localUser));
        }

        // multi-field sorting
        var sortBy = string.IsNullOrWhiteSpace(queryParams.SortBy) ? "UserName" : queryParams.SortBy;
        var sortFields = sortBy.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => typeof(UserProfileDto).GetProperties().Any(p => p.Name.Equals(f, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (sortFields.Count == 0)
            sortFields.Add("UserName");

        // Build dynamic sort
        var sortString = string.Join(", ", sortFields.Select(f => $"{f} {(queryParams.SortDescending ? "descending" : "ascending")}"));

        var sortedProfiles = userProfiles
            .AsQueryable()
            .OrderBy(sortString)
            .ToList();

        // Pagination
        var totalCount = sortedProfiles.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)queryParams.PageSize);
        var pagedProfiles = sortedProfiles
            .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToList();

        var pagedResult = new Dtos.PagedResult<UserProfileDto>
        {
            Items = pagedProfiles,
            CurrentPage = queryParams.PageNumber,
            PageSize = queryParams.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        return Result<Dtos.PagedResult<UserProfileDto>>.Ok(pagedResult);
    }

    public async Task<Result<UserProfileDto>> GetUserProfileByName(string username)
    {
        var keycloakUser = await _keycloakClientUser.GetUserByNameAsync(username);
        if (keycloakUser == null)
        {
            return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        var localUser = await _userRepository.GetByUsernameAsync(username);
        
        UserProfileDto profile = MapToUserProfile(keycloakUser, localUser);

        return Result<UserProfileDto>.Ok(profile);        
    }

    public async Task<Result<UserProfileDto>> GetUserProfileById(string userId)
    {
        var keycloakUser = await _keycloakClientUser.GetUserByIdAsync(userId);
        if (keycloakUser == null)
        {
            return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }            

        var localUser = await _userRepository.GetByUsernameAsync(keycloakUser.UserName ?? string.Empty);

        UserProfileDto profile = MapToUserProfile(keycloakUser, localUser);

        return Result<UserProfileDto>.Ok(profile);
    }

    public async Task<Result<List<UserProfileDto>>> GetUserProfilesByIds(List<IdDto> userIds)
    {
        var profiles = new List<UserProfileDto>();
        var notFoundIds = new List<string>();

        foreach (var userId in userIds)
        {
            var keycloakUser = await _keycloakClientUser.GetUserByIdAsync(userId.Id);
            if (keycloakUser == null)
            {
                notFoundIds.Add(userId.Id);
                continue;
            }

            var localUser = await _userRepository.GetByUsernameAsync(keycloakUser.UserName ?? string.Empty);
            profiles.Add(MapToUserProfile(keycloakUser, localUser));
        }

        if (profiles.Count == 0)
        {
            return _errors.Fail<List<UserProfileDto>>(ErrorCodes.AUTH.UsersNotFound);
        }

        var message = notFoundIds.Count > 0
            ? $"Some users not found: {string.Join(", ", notFoundIds)}"
            : "All users found.";

        return Result<List<UserProfileDto>>.Ok(profiles, message);
    }

    public async Task<Result<UserProfileDto>> CreateUserAsync(UserCreateDto request)
    {
        var mfaTypeString = _configuration["MfaType"] ?? "none";
        if (!Enum.TryParse<MfaType>(mfaTypeString, true, out var mfaType))
        {
            return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.InvalidMfaType);
        }

        // Check if user exists
        var keycloakUser = await _keycloakClientUser.GetUserByNameAsync(request.Username);
        if (keycloakUser != null)
        {
            return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.UsernameAlreadyExists);
        }

        // Password handling logic
        if (request.PasswordTemp)
        {
            if (string.IsNullOrWhiteSpace(request.Password))
                request.Password = Guid.NewGuid().ToString("N").Substring(0, 12);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Password))
                return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.PasswordRequired);
        }

        // Map UserCreateDto to KeycloakUserDto
        var keycloakUserDto = new KeycloakUserDto
        {
            Username = request.Username,
            Email = request.Email,
            Password = request.Password,
            PasswordTemp = request.PasswordTemp,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Enabled = request.Enabled,
            EmailVerified = request.EmailVerified
        };

        // Create User in Keycloak
        var keycloakUserNew = await _keycloakClientUser.CreateUserAsync(keycloakUserDto);
        if (keycloakUserNew == null)
        {
            return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.CreateUserInKeycloakFailed);
        }

        // Create User in DB
        var localUserNew = new User
        {
            KeycloakUserId = Guid.Parse(keycloakUserNew.Id),
            Username = keycloakUserNew.UserName,
            FirstName = keycloakUserNew.FirstName,
            LastName = keycloakUserNew.LastName,
            IsEnabled = keycloakUserNew.Enabled ?? request.Enabled,
            IsAdmin = request.IsAdmin,
            Email = keycloakUserNew.Email ?? request.Email,
            PhoneNumber = request.PhoneNumber ?? string.Empty,                       
            PhoneVerified = false,
            MfaType = mfaType,
            EmailVerified = keycloakUserNew.EmailVerified ?? request.EmailVerified,
            CreatedAt = DateTime.UtcNow
        };
        try
        {
            await _userRepository.AddAsync(localUserNew);
        }
        catch (Exception)
        {
            // Rollback Keycloak user if auth DB fails
            await _keycloakClientUser.DeleteUserAsync(keycloakUserNew.Id);
            return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.CreateUserInDbFailed);
        }

        // Assign admin role if IsAdmin is true
        if (request.IsAdmin)
        {
            var assignResult = await _roleManagementService.AssignRolesToUserAsync(
                request.Username,
                new List<RoleDto> { new RoleDto { RoleName = AdminRoleName } }
            );
            if (!assignResult.Success)
            {
                return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.AssignAdminFailed);
            }
        }

        // Map to UserProfileDto
        UserProfileDto profile = MapToUserProfile(keycloakUserNew, localUserNew);

        return Result<UserProfileDto>.Ok(profile, $"User {request.Username} created successfully");
    }

    public async Task<Result<UserProfileDto>> CreateUserWithRolesAsync(UserCreateDto  request, List<RoleDto> rolesToAssign)
    {
        // CreateUser 
        var keycloakUserNew = await CreateUserAsync(request);
        if (!keycloakUserNew.Success)
        {
            return _errors.Fail<UserProfileDto>(keycloakUserNew.ErrorCode ?? ErrorCodes.AUTH.GenericUnexpected);
        }

        // Assign roles   
        if (rolesToAssign != null && rolesToAssign.Count > 0)
        {
            var assignResult = await _roleManagementService.AssignRolesToUserAsync(request.Username, rolesToAssign);
            if (!assignResult.Success)
            {
                return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.AssignRoleFailed);
            }
        }
        
        return Result<UserProfileDto>.Ok(keycloakUserNew.Data!, $"User {request.Username} created and roles assigned successfully.");
    }

    public async Task<Result<UserProfileDto>> CreateUserAndSendEmailAsync(UserCreateDto request)
    {
        // CreateUser 
        var keycloakUserNew = await CreateUserAsync(request);
        if (!keycloakUserNew.Success)
        {
            return _errors.Fail<UserProfileDto>(keycloakUserNew.ErrorCode ?? ErrorCodes.AUTH.GenericUnexpected);
        }

        // Send Email to user  
        var result = await _passwordResetService.SendResetLinkAsync(request.Email);
        if (!result.Success)
        {
            return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.EmailVerificationSendFailed);
        }

        return Result<UserProfileDto>.Ok(keycloakUserNew.Data!, $"User {request.Username} created successfully & email sent.");
    }

    public async Task<Result<UserProfileDto>> UpdateUserAsync(UserUpdateDto request)
    {
        if (!Guid.TryParse(request.Id, out var userId))
            return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.UserIdNotValid);

        // Check if user exists
        var keycloakUser = await _keycloakClientUser.GetUserByIdAsync(request.Id);
        if (keycloakUser == null)
        {
            return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.UserIdNotFound);
        }

        // Map UserUpdateDto to KeycloakUserDto
        var keycloakUserDto = new KeycloakUserDto
        {
            Id = request.Id,
            Username = request.Username ?? keycloakUser.UserName,
            Email = request.Email ?? keycloakUser.Email,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Enabled = request.Enabled,
            EmailVerified = keycloakUser.EmailVerified ?? false,
        };

        // Update User in Keycloak
        var updateResult = await _keycloakClientUser.UpdateUserAsync(keycloakUserDto);
        if (!updateResult.Success)
        {
            return _errors.Fail<UserProfileDto>(updateResult.ErrorCode ?? ErrorCodes.AUTH.UpdateInKeycloakFailed);
        }

        // Update User in DB
        User? localUser = null;
        try
        {
            localUser = await _userRepository.GetByKeycloakUserIdAsync(userId);
            if (localUser != null)
            {
                localUser.Username = request.Username ?? localUser.Username;
                localUser.FirstName = request.FirstName ?? localUser.FirstName;
                localUser.LastName = request.LastName ?? localUser.LastName;
                localUser.Email = request.Email ?? localUser.Email;
                localUser.IsEnabled = request.Enabled ?? localUser.IsEnabled;
                localUser.PhoneNumber = request.PhoneNumber ?? localUser.PhoneNumber;
                localUser.IsAdmin = request.IsAdmin ?? localUser.IsAdmin;
                localUser.ModifiedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(localUser);
            }
        }
        catch (Exception)
        {
            var rollbackKeycloakDto = new KeycloakUserDto
            {
                Id = keycloakUser.Id,
                Username = keycloakUser.UserName,
                FirstName = keycloakUser.FirstName,
                LastName = keycloakUser.LastName,
                Email = keycloakUser.Email,
                Enabled = keycloakUser.Enabled
            };
            await _keycloakClientUser.UpdateUserAsync(rollbackKeycloakDto);

            return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.UpdateDbRolledBack);
        }

        // Update password if provided
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var passwordResult = await _keycloakClientUser.UpdateUserPasswordAsync(request.Id, request.Password, false);
            if (!passwordResult)
            {
                return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.UpdatePasswordFailed);
            }
        }

        // Handle admin role assignment/removal
        var rolesResult = await _roleManagementService.GetUserRolesAsync(request.Username ?? keycloakUser.UserName ?? string.Empty);
        var hasAdminRole = rolesResult.Success && rolesResult.Data?.Any(r => r.RoleName == AdminRoleName) == true;

        if (request.IsAdmin == true && !hasAdminRole)
        {
            var assignResult = await _roleManagementService.AssignRolesToUserAsync(
                request.Username ?? keycloakUser.UserName ?? string.Empty,
                new List<RoleDto> { new RoleDto { RoleName = AdminRoleName } }
            );
            if (!assignResult.Success)
            {
                return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.AssignAdminOnUpdateFailed);
            }
        }
        else if (!request.IsAdmin == true && hasAdminRole)
        {
            var removeResult = await _roleManagementService.RemoveRolesFromUserAsync(
                request.Username ?? keycloakUser.UserName ?? string.Empty,
                new List<RoleDto> { new RoleDto { RoleName = AdminRoleName } }
            );
            if (!removeResult.Success)
            {
                return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.RemoveAdminOnUpdateFailed);
            }
        }

        var updatedKeycloakUser = await _keycloakClientUser.GetUserByIdAsync(request.Id);

        // Map to UserProfileDto
        UserProfileDto profile = MapToUserProfile(updatedKeycloakUser, localUser);

        return Result<UserProfileDto>.Ok(profile, $"User {profile.UserName} updated successfully.");
    }

    public async Task<Result<bool>> DeleteUserAsync(string userId)
    {
        // Check if user exists
        var keycloakUser = await _keycloakClientUser.GetUserByIdAsync(userId);
        if (keycloakUser == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.DeleteTargetNotFound);
        }

        // Delete user from Keycloak
        var result = await _keycloakClientUser.DeleteUserAsync(keycloakUser.Id);
        if (!result)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.DeleteInKeycloakFailed);
        }

        // Delete user from auth DB if exists
        var localUser = await _userRepository.GetByKeycloakUserIdAsync(Guid.Parse(keycloakUser.Id));
        if (localUser != null)
        {
            await _userRepository.DeleteAsync(localUser);
        }

        return Result<bool>.Ok(data: true, message: $"User {userId} deleted successfully.");
    }
    
    public async Task<Result<IDictionary<string, string[]>>> GetUserAttributesAsync(string username)
    {
        var keycloakUser = await _keycloakClientUser.GetUserByNameAsync(username);
        if (keycloakUser == null)
        {
            return _errors.Fail<IDictionary<string, string[]>>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        var attributes = await _keycloakClientUser.GetUserAttributesAsync(keycloakUser.Id);

        return Result<IDictionary<string, string[]>>.Ok(attributes);
    }

    public async Task<Result<IDictionary<string, IDictionary<string, string[]>>>> GetUsersAttributesAsync(List<string> userIds)
    {
        var response = new Dictionary<string, IDictionary<string, string[]>>(StringComparer.OrdinalIgnoreCase);
        var notFound = new List<string>();

        foreach (var userId in userIds)
        {
            var keycloakUser = await _keycloakClientUser.GetUserByIdAsync(userId);
            if (keycloakUser == null)
            {
                notFound.Add(userId);
                continue;
            }

            var attributes = await _keycloakClientUser.GetUserAttributesAsync(keycloakUser.Id);
            response[userId] = attributes;
        }

        if (notFound.Count == userIds.Count)
        {
            return _errors.Fail<IDictionary<string, IDictionary<string, string[]>>>(ErrorCodes.AUTH.UsersNotFound);
        }

        return Result<IDictionary<string, IDictionary<string, string[]>>>.Ok(response);
    }

    public async Task<Result<bool>> SetUserAttributeAsync(string userId, string key, string value)
    {
        var keycloakUser = await _keycloakClientUser.GetUserByIdAsync(userId);
        if (keycloakUser == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        var result = await _keycloakClientUser.SetUserAttributeAsync(keycloakUser.Id, key, value);
        if (result == false)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.SetAttributeFailed);
        }

        return Result<bool>.Ok(data: true, message:$"Attribute {key} set successfully.");
    }

    public async Task<Result<bool>> DeleteUserAttributeAsync(string userId, string key)
    {
        var keycloakUser = await _keycloakClientUser.GetUserByIdAsync(userId);
        if (keycloakUser == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        var attributes = await _keycloakClientUser.GetUserAttributesAsync(keycloakUser.Id);
        if (!attributes.ContainsKey(key))
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.AttributeNotFound);
        }

        attributes.Remove(key);

        var result = await _keycloakClientUser.UpdateUserAttributesAsync(keycloakUser.Id, attributes);
        if (result == false)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.DeleteAttributeFailed);
        }

        return Result<bool>.Ok(data: true, message: $"Attribute {key} deleted successfully.");
    }

    public async Task<Result<bool>> ResetPasswordAndVerifyEmailAsync(PasswordResetDto request)
    {
        var result = await _passwordResetService.ResetPasswordAndVerifyEmailAsync(request.Token, request.NewPassword);
        if (!result.Success)
        {
            return _errors.Fail<bool>(result.ErrorCode ?? string.Empty);
        }
        return Result<bool>.Ok(data: true, result.Message);
    }

    public async Task<Result<bool>> EnableMfaAsync(string username, MfaType mfaType)
    {
        var dbUser = await _userRepository.GetByUsernameAsync(username);
        if (dbUser == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInDB);
        }
        
        dbUser.MfaType = mfaType;
        if (mfaType == MfaType.Email)
        {
            dbUser.EmailVerified = true;
        }

        await _userRepository.UpdateAsync(dbUser);

        return Result<bool>.Ok(data: true, message: "Mfa Set In DB");
    }

    public async Task<Result<bool>> DisableMfaAsync(string username)
    {
        var dbUser = await _userRepository.GetByUsernameAsync(username);
        if (dbUser == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInDB);
        }
        
        dbUser.MfaType = MfaType.None;
        await _userRepository.UpdateAsync(dbUser);

        return Result<bool>.Ok(data: true, message: "Mfa Set In DB");
    }

    public async Task<Result<bool>> EmailVerifiedAsync(string email)
    {
        var keycloakUser = await _keycloakClientUser.GetUserByEmailAsync(email);
        if (keycloakUser == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        var updateDto = new KeycloakUserDto
        {
            Id = keycloakUser.Id,
            EmailVerified = true,
        };

        var keycloakUpdateResult = await _keycloakClientUser.UpdateUserAsync(updateDto);
        if (!keycloakUpdateResult.Success)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UpdateInKeycloakFailed);
        }

        var dbUser = await _userRepository.GetByEmailAsync(email);
        if (dbUser == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInDB);
        }

        dbUser.EmailVerified = true;
        await _userRepository.UpdateAsync(dbUser);

        return Result<bool>.Ok(data: true, message: "Email Verified.");
    }

    public async Task<Result<bool>> PhoneVerifiedAsync(string phoneNumber)
    {
        var dbUser = await _userRepository.GetByPhoneNumberAsync(phoneNumber);
        if (dbUser == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInDB);
        }

        dbUser.PhoneVerified = true;
        await _userRepository.UpdateAsync(dbUser);

        return Result<bool>.Ok(data: true, message: "Phone Verified.");
    }

    private static UserProfileDto MapToUserProfile(KeycloakUser? keycloakUser, User? localUser)
    {
        if (keycloakUser == null)
        {
            throw new ArgumentNullException(nameof(keycloakUser), "KeycloakUser cannot be null.");
        }

        var profile = new UserProfileDto
        {
            // keycloak properties  
            Id = keycloakUser.Id,
            UserName = keycloakUser.UserName,
            Enabled = keycloakUser.Enabled,
            EmailVerified = keycloakUser.EmailVerified,
            FirstName = keycloakUser.FirstName,
            LastName = keycloakUser.LastName,
            Email = keycloakUser.Email,
            // Local properties  
            IsAdmin = localUser?.IsAdmin ?? false,
            PhoneNumber = localUser?.PhoneNumber,
            PhoneVerified = localUser?.PhoneVerified ?? false,            
            MfaMethod = localUser?.MfaType.ToString().ToLower(),
            CreatedAt = localUser?.CreatedAt ?? keycloakUser.CreatedAt
        };
        return profile;
    }
}
