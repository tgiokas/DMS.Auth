using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;

[ApiController]
[Route("api/user-management")]
public class UserManagementController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UserManagementController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet("users")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManagementService.GetUsersAsync();
        return Ok(users);
    }

    [HttpPost("create")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = await _userManagementService.CreateUserAsync(request);
        if (!result)
        {
            return BadRequest(new { message = "Failed to create user" });
        }
        return Ok(new { message = "User created successfully" });
    }

    [HttpDelete("delete/{username}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteUser(string username)
    {
        var result = await _userManagementService.DeleteUserAsync(username);
        if (!result)
        {
            return BadRequest(new { message = "Failed to delete user" });
        }
        return Ok(new { message = "User deleted successfully" });
    }

    [HttpGet("roles/{username}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetUserRoles(string username)
    {
        var roles = await _userManagementService.GetUserRolesAsync(username);
        return Ok(roles);
    }
}
