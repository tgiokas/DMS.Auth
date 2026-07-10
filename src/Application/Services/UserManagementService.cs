using System.Linq.Dynamic.Core;
using Microsoft.Extensions.Options;

using Authentication.Application.Configuration;
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
    private readonly IConfigurationService _configService;
    private readonly IUserRepository _userRepository;
    private readonly IEmailWhitelistRepository _emailWhitelistRepo;
    private readonly AuthSettings _authSettings;
    private readonly IErrorCatalog _errors;
    private readonly string AdminRoleName = "admin";

    public UserManagementService(
        IRoleManagementService roleManagementService,
        IPasswordResetService passwordResetService,
        IKeycloakClientUser keycloakClientUser,
        IConfigurationService configService,
        IUserRepository userRepository,
        IEmailWhitelistRepository emailWhitelistRepo,
        IOptions<AuthSettings> authOptions,
        IErrorCatalog errors)
    {
        _roleManagementService = roleManagementService;
        _passwordResetService = passwordResetService;
        _keycloakClientUser = keycloakClientUser;
        _configService = configService;
        _userRepository = userRepository;
        _emailWhitelistRepo = emailWhitelistRepo;
        _authSettings = authOptions.Value;
        _errors = errors;
    }

    public async Task<Result<Dtos.PagedResult<UserProfileDto>>> GetUsersAsync(UserQueryParams queryParams)
    {
        if (queryParams == null)
            queryParams = new UserQueryParams();

        var includeRoles = queryParams.IncludeRoles ?? true;

        // Build filters map
        var filtersMap = (queryParams.Filters ?? new List<FilterCriterion>())
            .ToDictionary(f => f.Field.Trim().ToLowerInvariant(), f => f.Value.Trim(), StringComparer.OrdinalIgnoreCase);

        string? searchVal = filtersMap.TryGetValue("search", out var sv) ? sv : null;
        bool hasSearch = !string.IsNullOrWhiteSpace(searchVal);

        // The local auth DB (users who have actually logged in, ~100 rows) is the
        // source of truth for the user list, NOT the full Keycloak/LDAP directory
        // (which can hold thousands of LDAP accounts that never used this app).
        // We only ever call Keycloak once per local user to enrich its profile.
        var localUsers = await _userRepository.GetNotDeletedAsync();
        localUsers = localUsers.Where(u => !string.IsNullOrEmpty(u.Username)).ToList();

        var userProfiles = await BuildUserProfilesAsync(localUsers);

        // No filters, sorting, or pagination --> return everything immediately
        bool noFilters = !filtersMap.Any();
        bool noSorting = queryParams.SortFields == null || !queryParams.SortFields.Any();
        bool noPaging = !queryParams.PageNumber.HasValue && !queryParams.PageSize.HasValue;

        if (noFilters && noSorting && noPaging)
        {
            // Keep roles only when requested, because enriching many users means many role-mapping calls.
            if (includeRoles)
                await EnrichRolesAsync(userProfiles);

            var resultAll = new Dtos.PagedResult<UserProfileDto>
            {
                Results = userProfiles,
                CurrentPage = 1,
                PageSize = userProfiles.Count,
                Total = userProfiles.Count,
                Pages = 1
            };
            return Result<Dtos.PagedResult<UserProfileDto>>.Ok(resultAll);
        }

        // Apply all filters in-memory against the (small) local user set.
        if (hasSearch)
        {
            var term = searchVal!.Trim();
            userProfiles = userProfiles
                .Where(u =>
                    Contains(u.UserName, term) ||
                    Contains(u.Email, term) ||
                    Contains(u.FirstName, term) ||
                    Contains(u.LastName, term))
                .ToList();
        }
        else if (queryParams.Filters != null && queryParams.Filters.Any())
        {
            if (filtersMap.TryGetValue("username", out var fUsername) && !string.IsNullOrWhiteSpace(fUsername))
                userProfiles = userProfiles.Where(u => Contains(u.UserName, fUsername)).ToList();

            if (filtersMap.TryGetValue("email", out var fEmail) && !string.IsNullOrWhiteSpace(fEmail))
                userProfiles = userProfiles.Where(u => Contains(u.Email, fEmail)).ToList();

            if (filtersMap.TryGetValue("firstname", out var fFirstName) && !string.IsNullOrWhiteSpace(fFirstName))
                userProfiles = userProfiles.Where(u => Contains(u.FirstName, fFirstName)).ToList();

            if (filtersMap.TryGetValue("lastname", out var fLastName) && !string.IsNullOrWhiteSpace(fLastName))
                userProfiles = userProfiles.Where(u => Contains(u.LastName, fLastName)).ToList();

            if (filtersMap.TryGetValue("enabled", out var fEnabled) && bool.TryParse(fEnabled, out var enabledBool))
                userProfiles = userProfiles.Where(u => u.Enabled == enabledBool).ToList();

            if (filtersMap.TryGetValue("isadmin", out var isAdminValue) && bool.TryParse(isAdminValue, out var isAdminBool))
            {
                userProfiles = userProfiles.Where(u => u.IsAdmin == isAdminBool).ToList();
            }

            // CreatedAtFrom / CreatedAtTo filters (inclusive)
            DateTime? createdFrom = null;
            DateTime? createdTo = null;

            if (filtersMap.TryGetValue("createdatfrom", out var fromValue) &&
                DateTime.TryParse(fromValue, out var parsedFrom))
            {
                createdFrom = parsedFrom.Date; // start of day
            }

            if (filtersMap.TryGetValue("createdatto", out var toValue) &&
                DateTime.TryParse(toValue, out var parsedTo))
            {
                createdTo = parsedTo.Date.AddDays(1).AddTicks(-1); // end of day
            }

            if (createdFrom.HasValue && createdTo.HasValue)
            {
                // Between range
                userProfiles = userProfiles
                    .Where(u => u.CreatedAt >= createdFrom.Value && u.CreatedAt <= createdTo.Value)
                    .ToList();
            }
            else if (createdFrom.HasValue)
            {
                // From only
                userProfiles = userProfiles
                    .Where(u => u.CreatedAt >= createdFrom.Value)
                    .ToList();
            }
            else if (createdTo.HasValue)
            {
                // To only
                userProfiles = userProfiles
                    .Where(u => u.CreatedAt <= createdTo.Value)
                    .ToList();
            }
        }

        // Sorting
        var sortBy = string.IsNullOrWhiteSpace(queryParams.SortFields)
            ? "UserName"
            : queryParams.SortFields;

        var sortFields = sortBy
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => typeof(UserProfileDto).GetProperties()
                .Any(p => p.Name.Equals(f, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // Always ensure at least one valid sort field
        if (!sortFields.Any())
            sortFields.Add("UserName");

        // Parse sort directions (aligned to sort fields)
        var directions = (queryParams.SortDirections ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Trim().ToLowerInvariant())
            .ToList();

        // Normalize list lengths 
        while (directions.Count < sortFields.Count)
            directions.Add(directions.LastOrDefault() ?? "ascending");

        // Fallback validation
        directions = directions
            .Select(d => d == "descending" ? "descending" : "ascending")
            .ToList();

        // Build combined sort string
        var sortString = string.Join(", ",
            sortFields.Select((f, i) => $"{f} {directions[i]}"));

        var sorted = userProfiles.AsQueryable().OrderBy(sortString).Cast<UserProfileDto>();

        // Pagination (return all if not provided)
        if (queryParams.PageNumber.HasValue && queryParams.PageSize.HasValue &&
            queryParams.PageNumber > 0 && queryParams.PageSize > 0)
        {
            sorted = sorted
                .Skip((queryParams.PageNumber.Value - 1) * queryParams.PageSize.Value)
                .Take(queryParams.PageSize.Value);
        }

        var items = sorted.ToList();

        // Enrich roles only for the page we return, not the whole directory.
        // High-volume consumers can set IncludeRoles = false to skip these
        // Keycloak role-mapping calls entirely.
        if (includeRoles)
            await EnrichRolesAsync(items);

        var totalCount = userProfiles.Count;
        var pageSize = queryParams.PageSize ?? totalCount;
        var totalPages = (pageSize > 0) ? (int)Math.Ceiling((double)totalCount / pageSize) : 1;
        var currentPage = queryParams.PageNumber ?? 1;

        var pagedResult = new Dtos.PagedResult<UserProfileDto>
        {
            Results = items,
            CurrentPage = currentPage,
            PageSize = pageSize,
            Total = totalCount,
            Pages = totalPages
        };

        return Result<Dtos.PagedResult<UserProfileDto>>.Ok(pagedResult);
    }      

    public async Task<Result<UserProfileDto>> GetUserByNameAsync(string username)
    {
        var keycloakUser = await _keycloakClientUser.GetUserByNameAsync(username);
        if (keycloakUser == null)
        {
            return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        var localUser = await _userRepository.GetByUsernameAsync(username);

        var rolesResult = await _roleManagementService.GetUserRolesAsync(keycloakUser.UserName);
        var roles = rolesResult.Success ? rolesResult.Data ?? new List<RoleProfileDto>() : new List<RoleProfileDto>();

        UserProfileDto profile = MapToUserProfile(keycloakUser, localUser, roles);

        return Result<UserProfileDto>.Ok(profile);
    }

    public async Task<Result<UserProfileDto>> GetUserByIdAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var guidUserId))
            return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.UserIdNotValid);

        var keycloakUser = await _keycloakClientUser.GetUserByIdAsync(userId);
        if (keycloakUser == null)
        {
            return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        var localUser = await _userRepository.GetByUsernameAsync(keycloakUser.UserName);

        var rolesResult = await _roleManagementService.GetUserRolesAsync(keycloakUser.UserName);
        var roles = rolesResult.Success ? rolesResult.Data ?? new List<RoleProfileDto>() : new List<RoleProfileDto>();

        UserProfileDto profile = MapToUserProfile(keycloakUser, localUser, roles);

        return Result<UserProfileDto>.Ok(profile);
    }

    public async Task<Result<List<UserProfileDto>>> GetUsersByIdsAsync(List<IdDto> userIds)
    {
        var userProfiles = new List<UserProfileDto>();
        var notFoundIds = new List<string>();

        foreach (var userId in userIds)
        {
            var keycloakUser = await _keycloakClientUser.GetUserByIdAsync(userId.Id);
            if (keycloakUser == null)
            {
                notFoundIds.Add(userId.Id);
                continue;
            }

            var localUser = await _userRepository.GetByUsernameAsync(keycloakUser.UserName);

            var rolesResult = await _roleManagementService.GetUserRolesAsync(keycloakUser.UserName);
            var roles = rolesResult.Success ? rolesResult.Data ?? new List<RoleProfileDto>() : new List<RoleProfileDto>();

            userProfiles.Add(MapToUserProfile(keycloakUser, localUser, roles));
        }

        if (userProfiles.Count == 0)
        {
            return _errors.Fail<List<UserProfileDto>>(ErrorCodes.AUTH.UsersNotFound);
        }

        var message = notFoundIds.Count > 0
            ? $"Some users not found: {string.Join(", ", notFoundIds)}"
            : "All users found.";

        return Result<List<UserProfileDto>>.Ok(userProfiles, message);
    }

    public async Task<Result<List<UserInfoDto>>> IsDeletedAsync(List<IdDto> userIds)
    {
        var users = new List<UserInfoDto>();
        var ids = new List<Guid>();

        userIds.ForEach(x =>
        {
            if (Guid.TryParse(x.Id, out var guidUserId))
            {
                ids.Add(guidUserId);
            }
        });

        if (ids.Count > 0)
        {
            var response = await _userRepository.AreDeletedAsync(ids);

            users.AddRange(response.Select(r => new UserInfoDto
            {
                Id = r.KeycloakUserId.ToString(),
                IsDeleted = r.IsDeleted
            }));
        }

        return Result<List<UserInfoDto>>.Ok(users);
    }

    public async Task<Result<UserProfileDto>> CreateUserAsync(UserCreateDto request)
    {
        request.Username = request.Username.Trim().ToLowerInvariant();
        request.Email = request.Email.Trim().ToLowerInvariant();

        // Check email whitelist if enabled
        var whitelistTypeValue = _authSettings.EmailsWhitelist;
        if (!whitelistTypeValue.Equals("off", StringComparison.CurrentCultureIgnoreCase))
        {
            var isWhitelisted = await _emailWhitelistRepo.IsWhitelistedAsync(request.Email);
            if (!isWhitelisted)
            {
                return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.EmailNotWhitelisted);
            }
        }

        // Check if user exists
        var keycloakUser = await _keycloakClientUser.GetUserByNameAsync(request.Username);
        if (keycloakUser != null)
        {
            var localUser = await _userRepository.GetByUsernameAsync(request.Username);
            if (localUser != null && localUser.IsDeleted)
                return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.UsernameIsDeleted);
            else
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
        var mfaType = await _configService.GetMfaTypeAsync();
        var localUserNew = new User
        {
            KeycloakUserId = Guid.Parse(keycloakUserNew.Id),
            Username = keycloakUserNew.UserName,
            PhoneNumber = request.PhoneNumber,
            IsAdmin = request.IsAdmin,
            MfaType = Enum.Parse<MfaType>(mfaType.Data ?? "None"),
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

    public async Task<Result<UserProfileDto>> CreateUserWithRolesAsync(UserCreateDto request, List<RoleDto> rolesToAssign)
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

        // Send Email with reset link to user  
        var result = await _passwordResetService.SendResetLinkAsync(request.Email, EmailTemplateType.InitialPasswordSet);
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

        request.Username = request.Username?.ToLower() ?? null;
        request.Email = request.Email?.ToLower() ?? null;

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
                localUser.PhoneNumber = request.PhoneNumber ?? localUser.PhoneNumber;
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

            return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.UpdateInDBFailed);
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
        var rolesResult = await _roleManagementService.GetUserRolesAsync(request.Username ?? keycloakUser.UserName);
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
        else if (request.IsAdmin == false && hasAdminRole)
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

        if (updatedKeycloakUser == null)
        {
            return _errors.Fail<UserProfileDto>(ErrorCodes.AUTH.UserIdNotFound);
        }

        // Map to UserProfileDto
        UserProfileDto profile = MapToUserProfile(updatedKeycloakUser, localUser);

        return Result<UserProfileDto>.Ok(profile, $"User {profile.UserName} updated successfully.");
    }

    public async Task<Result<bool>> DeleteUsersAsync(List<IdDto> userIds)
    {
        var failedIds = new List<string>();

        foreach (var userIdDto in userIds)
        {
            // Check if user exists
            var keycloakUser = await _keycloakClientUser.GetUserByIdAsync(userIdDto.Id);
            if (keycloakUser == null)
            {
                failedIds.Add(userIdDto.Id);
                continue;
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
        }

        if (failedIds.Count > 0)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.DeleteTargetNotFound);
        }

        return Result<bool>.Ok(data: true, message: "Users deleted successfully.");
    }

    public async Task<Result<bool>> FakeDeleteUsersAsync(List<IdDto> userIds)
    {
        var failedIds = new List<string>();

        foreach (var userIdDto in userIds)
        {
            // Check if user exists
            var keycloakUser = await _keycloakClientUser.GetUserByIdAsync(userIdDto.Id);
            if (keycloakUser == null)
            {
                failedIds.Add(userIdDto.Id);
                continue;
            }

            var updateDto = new KeycloakUserDto
            {
                Id = userIdDto.Id,
                Enabled = false
            };

            // Fake Delete (i.e. Disabled) user in Keycloak
            var keycloakUpdateResult = await _keycloakClientUser.UpdateUserAsync(updateDto);
            if (!keycloakUpdateResult.Success)
            {
                failedIds.Add(userIdDto.Id);
                continue;
            }

            // Fake Delete (i.e. is_deleted = true) user in db
            var localUser = await _userRepository.GetByKeycloakUserIdAsync(Guid.Parse(keycloakUser.Id));
            if (localUser != null)
            {
                localUser.ModifiedAt = DateTime.UtcNow;
                localUser.IsDeleted = true;
                await _userRepository.UpdateAsync(localUser);
            }
        }

        if (failedIds.Count > 0)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.DeleteTargetNotFound);
        }

        return Result<bool>.Ok(data: true, message: "Users Fake deleted successfully.");
    }

    public async Task<Result<bool>> EnableUsersAsync(List<IdDto> userIds)
    {
        var failedIds = new List<string>();

        foreach (var userIdDto in userIds)
        {
            // Check if user exists
            var keycloakUser = await _keycloakClientUser.GetUserByIdAsync(userIdDto.Id);
            if (keycloakUser == null)
            {
                failedIds.Add(userIdDto.Id);
                continue;
            }

            var updateDto = new KeycloakUserDto
            {
                Id = userIdDto.Id,
                Enabled = true
            };

            // Enable user in Keycloak
            var keycloakUpdateResult = await _keycloakClientUser.UpdateUserAsync(updateDto);
            if (!keycloakUpdateResult.Success)
            {
                failedIds.Add(userIdDto.Id);
                continue;
            }

            // update modified_at in db
            var localUser = await _userRepository.GetByKeycloakUserIdAsync(Guid.Parse(keycloakUser.Id));
            if (localUser != null)
            {
                localUser.ModifiedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(localUser);
            }
        }

        if (failedIds.Count > 0)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        return Result<bool>.Ok(data: true, message: "Users enabled successfully.");
    }

    public async Task<Result<bool>> DisableUsersAsync(List<IdDto> userIds)
    {
        var failedIds = new List<string>();

        foreach (var userIdDto in userIds)
        {
            // Check if user exists
            var keycloakUser = await _keycloakClientUser.GetUserByIdAsync(userIdDto.Id);
            if (keycloakUser == null)
            {
                failedIds.Add(userIdDto.Id);
                continue;
            }

            var updateDto = new KeycloakUserDto
            {
                Id = userIdDto.Id,
                Enabled = false
            };

            // Disable user in Keycloak
            var keycloakUpdateResult = await _keycloakClientUser.UpdateUserAsync(updateDto);
            if (!keycloakUpdateResult.Success)
            {
                failedIds.Add(userIdDto.Id);
                continue;
            }

            // update modified_at in db
            var localUser = await _userRepository.GetByKeycloakUserIdAsync(Guid.Parse(keycloakUser.Id));
            if (localUser != null)
            {
                localUser.ModifiedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(localUser);
            }
        }

        if (failedIds.Count > 0)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInKeycloak);
        }

        return Result<bool>.Ok(data: true, message: "Users disabled successfully.");
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

    public async Task<Result<IDictionary<string, IDictionary<string, string[]>>>> GetUsersAttributesAsync(List<UserIdDto> userIds)
    {
        var response = new Dictionary<string, IDictionary<string, string[]>>(StringComparer.OrdinalIgnoreCase);
        var notFound = new List<string>();

        foreach (var idDto in userIds)
        {
            var keycloakUser = await _keycloakClientUser.GetUserByIdAsync(idDto.UserId);
            if (keycloakUser == null)
            {
                notFound.Add(idDto.UserId);
                continue;
            }

            var attributes = await _keycloakClientUser.GetUserAttributesAsync(keycloakUser.Id);
            response[idDto.UserId] = attributes;
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

        return Result<bool>.Ok(data: true, message: $"Attribute {key} set successfully.");
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

    /// Builds a UserProfileDto for each local (auth-db) user by fetching its matching
    /// Keycloak profile individually, instead of paging through the entire LDAP directory.
    /// Runs with bounded concurrency so a large local user set does not open a connection per user.
    private async Task<List<UserProfileDto>> BuildUserProfilesAsync(List<User> localUsers)
    {
        if (localUsers.Count == 0)
            return new List<UserProfileDto>();

        const int maxConcurrency = 10;
        using var gate = new SemaphoreSlim(maxConcurrency);

        var tasks = localUsers.Select(async localUser =>
        {
            await gate.WaitAsync();
            try
            {
                var keycloakUser = await _keycloakClientUser.GetUserByNameAsync(localUser.Username);
                return keycloakUser == null ? null : MapToUserProfile(keycloakUser, localUser);
            }
            finally
            {
                gate.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.Where(p => p != null).Select(p => p!).ToList();
    }

    private static bool Contains(string? source, string term)
        => !string.IsNullOrEmpty(source) && source.Contains(term, StringComparison.OrdinalIgnoreCase);

    /// Populates UserProfileDto.Roles for the given profiles by fetching each user's client roles 
    /// Runs with bounded concurrency so a "return everything" call does not open a connection per user.
    private async Task EnrichRolesAsync(IReadOnlyCollection<UserProfileDto> profiles)
    {
        if (profiles.Count == 0)
            return;

        const int maxConcurrency = 10;
        using var gate = new SemaphoreSlim(maxConcurrency);

        var tasks = profiles.Select(async profile =>
        {
            await gate.WaitAsync();
            try
            {
                var rolesResult = await _roleManagementService.GetUserRolesByUserIdAsync(profile.Id);
                profile.Roles = rolesResult.Success
                    ? (rolesResult.Data ?? new List<RoleProfileDto>())
                    : new List<RoleProfileDto>();
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    private static UserProfileDto MapToUserProfile(KeycloakUser keycloakUser,
        User? localUser,
        List<RoleProfileDto>? roles = null)
    {
        var profile = new UserProfileDto
        {
            Id = keycloakUser.Id,
            UserName = keycloakUser.UserName,
            Enabled = keycloakUser.Enabled,
            EmailVerified = keycloakUser.EmailVerified,
            FirstName = keycloakUser.FirstName,
            LastName = keycloakUser.LastName,
            Email = keycloakUser.Email,
            PhoneNumber = localUser?.PhoneNumber,
            Deleted = localUser?.IsDeleted ?? false,
            IsAdmin = localUser?.IsAdmin ?? false,
            MfaMethod = localUser?.MfaType.ToString().ToLower(),
            CreatedAt = localUser?.CreatedAt ?? keycloakUser.CreatedAt,
            ModifiedAt = localUser?.ModifiedAt,
            Roles = roles ?? new List<RoleProfileDto>(),
            Attributes = keycloakUser.Attributes
        };
        return profile;
    }
}
