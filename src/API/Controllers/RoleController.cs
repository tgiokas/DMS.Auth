using Microsoft.AspNetCore.Mvc;

using Authentication.Application.Interfaces;
using Authentication.Application.Dtos;

namespace Authentication.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class RoleController : ControllerBase
{
    private readonly IRoleManagementService _roleManagementService;

    public RoleController(IRoleManagementService roleManagementService)
    {
        _roleManagementService = roleManagementService;
    }

    [HttpPost("getroles")]
    public async Task<IActionResult> GetRoles()
    {
        var result = await _roleManagementService.GetRolesAsync();
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("getbyname")]
    public async Task<IActionResult> GetRoleByName(RoleDto request)
    {
        var result = await _roleManagementService.GetRoleByNameAsync(request.RoleName);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("getbyid")]
    public async Task<IActionResult> GetRoleById(IdDto request)
    {
        var result = await _roleManagementService.GetRoleByIdAsync(request.Id);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("getuserroles")]
    public async Task<IActionResult> GetUserRoles(UserNameDto request)
    {
        var result = await _roleManagementService.GetUserRolesAsync(request.Username);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateRole(RoleDto request)
    {
        var result = await _roleManagementService.CreateRoleAsync(request);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdateRole(RoleUpdateDto request)
    {
        var result = await _roleManagementService.UpdateRoleAsync(request);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("delete")]
    public async Task<IActionResult> DeleteRole(RoleDto request)
    {
        var result = await _roleManagementService.DeleteRoleAsync(request.RoleName);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("assign")]
    public async Task<IActionResult> AssignRolesToUser(RoleAssignDto request)
    {
        var result = await _roleManagementService.AssignRolesToUserAsync(request.Username, request.Roles);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("remove")]
    public async Task<IActionResult> RemoveRolesFromUser(RoleAssignDto request)
    {
        var result = await _roleManagementService.RemoveRolesFromUserAsync(request.Username, request.Roles);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }   
}
