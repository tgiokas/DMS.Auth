using Microsoft.AspNetCore.Mvc;

using Authentication.Application.Services;
using Authentication.Application.Dtos;

namespace Authentication.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class RolePermissionController : ControllerBase
{
    private readonly RolePermissionService _rolePermissionService;

    public RolePermissionController(RolePermissionService service)
    {
        _rolePermissionService = service;
    }

    [HttpPost("getall")]
    public async Task<IActionResult> GetAllAsync()
    {
        var result = await _rolePermissionService.GetAllAsync();
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("getbyid")]
    public async Task<IActionResult> GetById(RolePermissionIdDto request)
    {
        var result = await _rolePermissionService.GetByIdAsync(request.Id);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("getbyroleid")]
    public async Task<IActionResult> GetByRoleId(RoleIdDto request)
    {
        var result = await _rolePermissionService.GetByRoleIdAsync(request.RoleId);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(List<RolePermissionCreateDto> request)
    {
        var result = await _rolePermissionService.AddAsync(request);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update(RolePermissionUpdateDto request)
    {
        var result = await _rolePermissionService.UpdateAsync(request);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete(List<RolePermissionIdDto> request)
    {
        var result = await _rolePermissionService.DeleteAsync(request);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }
}
