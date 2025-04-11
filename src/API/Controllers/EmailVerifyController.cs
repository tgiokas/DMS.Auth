using Microsoft.AspNetCore.Mvc;

using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;

namespace Authentication.Api.Controllers;

[ApiController]
[Route("api/email")]
public class EmailVerifyController : ControllerBase
{
    private readonly IEmailVerificationService _emailService;

    public EmailVerifyController(IEmailVerificationService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("send-verification-email")]
    public async Task<IActionResult> SendEmailVerification([FromBody] EmailVerificationDto request)
    {
        await _emailService.SendVerificationEmailAsync(request.Email);
        return Ok(new { message = "Verification email sent" });
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var result = await _emailService.VerifyEmailAsync(token);
        if (!result)
            return BadRequest(new { message = "Invalid or expired verification link" });

        return Ok(new { message = "Email verified successfully" });
    }
}
