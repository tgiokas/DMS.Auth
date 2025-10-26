using Microsoft.AspNetCore.Mvc;

using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;

namespace Authentication.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UserController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpPost("getusers")]
    public async Task<IActionResult> GetUsers(UserQueryParams queryParams)
    {
        var result = await _userManagementService.GetUsersAsync(queryParams);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("getbyname")]
    public async Task<IActionResult> GetUserByName(UserNameDto request)
    {
        var result = await _userManagementService.GetUserProfileByName(request.Username);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("getbyid")]
    public async Task<IActionResult> GetUserById(IdDto request)
    {
        var result = await _userManagementService.GetUserProfileById(request.Id);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("getbyids")]
    public async Task<IActionResult> GetUserByIds(List<IdDto> request)
    {
        var result = await _userManagementService.GetUserProfilesByIds(request);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateUser(UserCreateDto request)
    {
        var result = await _userManagementService.CreateUserAsync(request);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("create-with-role")]
    public async Task<IActionResult> CreateUserWithRole(UserCreateWithRolesDto request)
    {
        var result = await _userManagementService.CreateUserWithRolesAsync(request.User, request.Roles);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("create-register")]
    public async Task<IActionResult> CreateAndSendEmail(UserCreateDto request)
    {
        var result = await _userManagementService.CreateUserAndSendEmailAsync(request);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("verify")]
    public async Task<IActionResult> ResetPasswordAndVerifyEmailAsync(PasswordResetDto request)
    {
        var result = await _userManagementService.ResetPasswordAndVerifyEmailAsync(request);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdateUser(UserUpdateDto request)
    {
        var result = await _userManagementService.UpdateUserAsync(request);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("delete")]
    public async Task<IActionResult> DeleteUser(IdDto request)
    {
        var result = await _userManagementService.DeleteUserAsync(request.Id);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("attributes")]
    public async Task<IActionResult> GetUserAttributes(List<UserIdDto> request)
    {
        var userIds = request.Select(r => r.UserId).ToList();
        var result = await _userManagementService.GetUsersAttributesAsync(userIds);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("attributes/set")]
    public async Task<IActionResult> SetUserAttribute(UserAttributeDto request)
    {
        var result = await _userManagementService.SetUserAttributeAsync(request.UserId, request.Key, request.Value);
        if (!result.Success)
        {
            return Accepted(result);
        }
        return Ok(result);
    }

    [HttpPost("attributes/delete")]
    public async Task<IActionResult> DeleteUserAttribute(UserAttributeDto request)
    {
        var result = await _userManagementService.DeleteUserAttributeAsync(request.UserId, request.Key);
        if (!result.Success)
        {
            return Accepted(result);             
        }
        return Ok(result);
    }
}
