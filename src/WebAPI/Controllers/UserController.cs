using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;

namespace DMS.Auth.WebApi;

[ApiController]
[Route("api/user")]
public class UserController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UserController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }      
    
    [HttpGet("users")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetUsers()
    {
        var response = await _userManagementService.GetUsersAsync();
        if (response == null)
        {
            return BadRequest(new { message = "Failed to get Users" });
        }
        return Ok(response);
    }

    [HttpGet("profile")]
    [Authorize(Roles = "admin")]
    public IActionResult GetUserProfile(string username)
    {
        return Ok(new { Id = 1, Username = username, Email = "testuser@example.com" });
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateUser([FromBody] UserCreateDto request)
    {        
        var response = await _userManagementService.CreateUserAsync(request);
        if (!response)
        {
            return BadRequest(new { message = "Failed to create user" });
        }
        return Ok(new { message = "User created successfully" });
    }

    [HttpPut("update/{username}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateUser([FromBody] UserUpdateDto request)
    {
        var response = await _userManagementService.UpdateUserAsync(request);
        if (!response)
        {
            return BadRequest(new { message = "Failed to update user" });
        }
        return Ok(new { message = "User updated successfully" });
    }

    [HttpDelete("delete/{username}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteUser(string username)
    {
        var response = await _userManagementService.DeleteUserAsync(username);
        if (!response)
        {
            return BadRequest(new { message = "Failed to delete user" });
        }
        return Ok(new { message = "User deleted successfully" });
    }

    [HttpGet("roles/{username}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetUserRoles(string username)
    {
        var response = await _userManagementService.GetUserRolesAsync(username);
        if (response == null)
        {
            return BadRequest(new { message = "Failed to get Roles" });
        }
        return Ok(response);
    }
}
