﻿using Microsoft.Extensions.Configuration;
using AutoMapper;
using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;
using DMS.Auth.Domain.Interfaces;

namespace DMS.Auth.Application.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IKeycloakClient _keycloakClient;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;
    //private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(IKeycloakClient keycloakClient, IUserRepository userRepository, IConfiguration configuration, IMapper mapper)
    {
        _keycloakClient = keycloakClient;
        _userRepository = userRepository;
        _configuration = configuration;
        _mapper = mapper;
    }

    public async Task<List<KeycloakUser>> GetUsersAsync()
    {
        return await _keycloakClient.GetUsersAsync();
    }

    public async Task<bool> CreateUserAsync(CreateUserDto request)
    {

        bool storeInLocalDb = bool.Parse(_configuration["StoreUsersInLocalDb"] ?? "false");

        //if (storeInLocalDb)
        //{
        //    var user = _mapper.Map<User>(request);
        //    await _userRepository.AddAsync(user);
        //}       

        return await _keycloakClient.CreateUserAsync(request.Username, request.Email, request.Password);        
    }

    public async Task<bool> UpdateUserAsync(UpdateUserDto request)
    {
        return await _keycloakClient.UpdateUserAsync(request);
    }
    
    public async Task<bool> DeleteUserAsync(string username)
    {
        return await _keycloakClient.DeleteUserAsync(username);
    }

    public async Task<List<KeycloakRole>> GetUserRolesAsync(string username)
    {
        return await _keycloakClient.GetUserRolesAsync(username);
    }

    public async Task<bool> AssignRoleAsync(string username, string roleId)
    {
        return await _keycloakClient.AssignRoleAsync(username, roleId);
    }    

    public async Task<bool> EnableMfaAsync(string username)
    {
        return await _keycloakClient.EnableMfaAsync(username);
    }
}
