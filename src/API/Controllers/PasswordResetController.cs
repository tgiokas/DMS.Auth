using Microsoft.AspNetCore.Mvc;

using Authentication.Application.Interfaces;
using Authentication.Application.Dtos;
using Authentication.Domain.Enums;

namespace Authentication.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class PasswordResetController : ControllerBase
{
    private readonly IPasswordResetService _passwordResetService;

    public PasswordResetController(IPasswordResetService passwordResetService)
    {
        _passwordResetService = passwordResetService;
    }

    [HttpPost("set")]
    public async Task<IActionResult> SetPassword(EmailAddressDto request)
    {
        var result = await _passwordResetService.SendResetLinkAsync(request.Email, EmailTemplateType.InitialPasswordSet);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("forgot")]
    public async Task<IActionResult> ForgotPassword(EmailAddressDto request)
    {
        var result = await _passwordResetService.SendResetLinkAsync(request.Email, EmailTemplateType.PasswordReset);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("reset")]
    public async Task<IActionResult> ResetPassword(PasswordResetDto request)
    {
        var result = await _passwordResetService.ResetPasswordAsync(request.Token, request.NewPassword);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }
}