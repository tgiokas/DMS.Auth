using Microsoft.AspNetCore.Mvc;

using Authentication.Application.Interfaces;
using Authentication.Application.Dtos;

namespace Authentication.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class ForgotPasswordController : ControllerBase
{
    private readonly IPasswordForgotService _forgotPasswordService;

    public ForgotPasswordController(IPasswordForgotService forgotPasswordService)
    {
        _forgotPasswordService = forgotPasswordService;
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] PasswordForgotDto request)
    {
        await _forgotPasswordService.SendResetLinkAsync(request.Email);
        return Ok(new { message = "If this email exists, a reset link was sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] PasswordResetDto request)
    {
        var success = await _forgotPasswordService.ResetPasswordAsync(request.Token, request.NewPassword);

        if (!success)
            return BadRequest(new { message = "Invalid or expired token" });

        return Ok(new { message = "Password reset successfully" });
    }
}