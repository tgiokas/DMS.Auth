using Microsoft.Extensions.Configuration;

using AutoMapper;

using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;
using DMS.Auth.Domain.Interfaces;
using DMS.Auth.Domain.Entities;

namespace DMS.Auth.Application.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IKeycloakClient _keycloakClient;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;
    //private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(IKeycloakClient keycloakClient, 
        IUserRepository userRepository, 
        IConfiguration configuration, 
        IMapper mapper)
    {
        _keycloakClient = keycloakClient;
        _userRepository = userRepository;
        _configuration = configuration;
        _mapper = mapper;
    }

    public async Task<List<KeycloakUserDto>> GetUsersAsync()
    {
        return await _keycloakClient.GetUsersAsync();
    }

    //public async Task<KeycloakUserDto> GetUserProfile(string username)
    //{
    //    return await _keycloakClient.GetUserIdByUsernameAsync(username);
    //}

    public async Task<bool> CreateUserAsync(UserCreateDto request)
    {

        bool storeInLocalDb = bool.Parse(_configuration["StoreUsersInLocalDb"] ?? "false");

        if (storeInLocalDb)
        {
            var user = _mapper.Map<User>(request);
            await _userRepository.AddAsync(user);
        }

        return await _keycloakClient.CreateUserAsync(request.Username, request.Email, request.Password);        
    }

    public async Task<bool> UpdateUserAsync(UserUpdateDto request)
    {
        return await _keycloakClient.UpdateUserAsync(request);
    }
    
    public async Task<bool> DeleteUserAsync(string username)
    {
        return await _keycloakClient.DeleteUserAsync(username);
    }

    public async Task<List<KeycloakRoleDto>> GetUserRolesAsync(string username)
    {
        return await _keycloakClient.GetUserRolesAsync(username);
    }

    public async Task<bool> AssignRoleAsync(string username, string roleId)
    {
        return await _keycloakClient.AssignRoleAsync(username, roleId);
    }    

    public Task<bool> EnableMfaAsync(string username)
    {
        //return await _keycloakClient.EnableMfaAsync(username);
        return Task.FromResult(true);
    }
}
