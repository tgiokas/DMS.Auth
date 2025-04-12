using Microsoft.Extensions.Configuration;

using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;
using Authentication.Domain.Interfaces;
using Authentication.Domain.Entities;

namespace Authentication.Application.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IKeycloakClient _keycloakClient;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;  

    public UserManagementService(IKeycloakClient keycloakClient, 
        IUserRepository userRepository, 
        IConfiguration configuration)
    {
        _keycloakClient = keycloakClient;
        _userRepository = userRepository;
        _configuration = configuration;        
    }

    public async Task<List<KeycloakUser>?> GetUsersAsync()
    {
        return await _keycloakClient.GetUsersAsync();
    }

    public async Task<KeycloakUser?> GetUserProfile(string username)
    {
        return await _keycloakClient.GetUserProfileAsync(username);       
    }

    public async Task<bool> CreateUserAsync(UserCreateDto request)
    {
        var keyclaokUserId = await _keycloakClient.CreateUserAsync(request.Username, request.Email, request.Password);

        bool storeInLocalDb = bool.Parse(_configuration["StoreUsersInLocalDb"] ?? "false");

        if (!string.IsNullOrEmpty(keyclaokUserId))
        {
            if (storeInLocalDb)
            {
                var user = new User
                {
                    KeycloakUserId = keyclaokUserId,
                    Username = request.Username,
                    Email = request.Email,
                    IsMfaEnabled = request.IsMfaEnabled ?? false
                };
                await _userRepository.AddAsync(user);
            }
            return true;
        }
        else
        {
            return false;
        }
    } 

    public async Task<bool> UpdateUserAsync(UserUpdateDto request)
    {
        return await _keycloakClient.UpdateUserAsync(request);
    }
    
    public async Task<bool> DeleteUserAsync(string username)
    {
        return await _keycloakClient.DeleteUserAsync(username);
    }

    public async Task<List<KeycloakRole>?> GetUserRolesAsync(string username)
    {
        return await _keycloakClient.GetUserRolesAsync(username);
    }

    public async Task<bool> AssignRoleAsync(string username, string roleId)
    {
        return await _keycloakClient.AssignRoleAsync(username, roleId);
    }    

    public Task<bool> EnableMfaAsync(string username)
    {
        return Task.FromResult(true);
    }

    public async Task MarkPhoneAsVerifiedAsync(string phoneNumber)
    {
        var user = await _userRepository.GetByPhoneNumberAsync(phoneNumber);
        if (user == null) throw new Exception("User not found");

        user.PhoneVerified = true;
        await _userRepository.UpdateAsync(user);
    }

    public async Task MarkEmailAsVerifiedAsync(string email)
    {
        var user = await _userRepository.GetByEmailNumberAsync(email);
        if (user == null) throw new Exception("User not found");

        user.EmailVerified = true;
        await _userRepository.UpdateAsync(user);
    }
}
