using System.Security.Claims;
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
        var users = await _userManagementService.GetUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "admin")]
    public IActionResult GetUserById(int id)
    {
        return Ok(new { Id = id, Username = "user" + id, Email = $"user{id}@example.com" });
    }

    [HttpGet("profile")]
    [Authorize]
    public IActionResult GetUserProfile(string username)
    {
        return Ok(new { Id = 1, Username = username, Email = "testuser@example.com" });
    }

    [HttpPost("create")]   
    //[Authorize(Policy = "AdminOnly")]
    //[Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto request)
    {
        var accessToken = HttpContext.Items["AccessToken"] as string;

        var result = await _userManagementService.CreateUserAsync(request);
        if (!result)
        {
            return BadRequest(new { message = "Failed to create user" });
        }
        return Ok(new { message = "User created successfully" });
    }

    [HttpDelete("update/{username}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto request)
    {
        var result = await _userManagementService.UpdateUserAsync(request);
        if (!result)
        {
            return BadRequest(new { message = "Failed to update user" });
        }
        return Ok(new { message = "User updated successfully" });
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

    //[HttpGet("token-info")]
    //[Authorize] // Requires valid JWT Token
    //public IActionResult GetTokenInfo()
    //{
    //    var identity = User.Identity as ClaimsIdentity;

    //    if (identity == null)
    //        return Unauthorized("No identity found.");

    //    var claims = identity.Claims
    //        .Select(c => new { c.Type, c.Value })
    //        .ToList();

    //    Console.WriteLine("===== Extracted Claims =====");
    //    foreach (var claim in claims)
    //    {
    //        Console.WriteLine($"- {claim.Type}: {claim.Value}");
    //    }
    //    Console.WriteLine("============================");

    //    return Ok(claims);
    //}
}
