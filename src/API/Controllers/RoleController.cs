using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Authentication.Application.Interfaces;

namespace Authentication.Api.Controllers;

public class RoleController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    public RoleController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet("roles/{username}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetRoles()
    {
        var users = await _userManagementService.GetUsersAsync();
        return Ok(users);
    }
}
