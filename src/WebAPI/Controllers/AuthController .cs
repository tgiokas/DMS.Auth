using Microsoft.AspNetCore.Mvc;
using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _userAppService;

    public AuthController(IAuthenticationService userAppService)
    {
        _userAppService = userAppService;
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        var userDto = await _userAppService.CreateUserAsync(request);
        return Ok(userDto);
    }

    // Update user
    [HttpPut("{userId}")]
    public async Task<ActionResult<UserDto>> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request)
    {
        var updatedUser = await _userAppService.UpdateUserAsync(userId, request);
        return Ok(updatedUser);
    }

        
    // Delete user        
    [HttpDelete("{userId}")]        
    public async Task<IActionResult> DeleteUser(Guid userId)        
    {            
        await _userAppService.DeleteUserAsync(userId);            
        return NoContent();        
    }
    

    // Assign role
    [HttpPost("{userId}/assign-role/{roleName}")]
    public async Task<IActionResult> AssignRole(Guid userId, string roleName)
    {
        await _userAppService.AssignRoleAsync(userId, roleName);
        return NoContent();
    }

    // Enable MFA
    [HttpPost("{userId}/enable-mfa")]
    public async Task<IActionResult> EnableMfa(Guid userId)
    {
        await _userAppService.EnableMfaAsync(userId);
        return NoContent();
    }
}