using Microsoft.AspNetCore.Mvc;

using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;

namespace Authentication.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class EmailVerifyController : ControllerBase
{
    private readonly IEmailVerificationService _emailService;

    public EmailVerifyController(IEmailVerificationService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("send-email")]
    public async Task<IActionResult> SendEmailVerification(EmailDto request)
    {
        var result = await _emailService.SendVerificationLinkAsync(request.Email);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(EmailVerificationDto request)
    {
        var result = await _emailService.VerifyEmailLinkAsync(request.Token);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("send-code")]
    public async Task<IActionResult> SendEmailCode(EmailDto request)
    {
        var result = await _emailService.SendVerificationCodeAsync(request.Email);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("verify-code")]
    public async Task<IActionResult> VerifyEmailCode(EmailCodeDto request)
    {
        var result = await _emailService.VerifyEmailCodeAsync(request.Email, request.Code);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }
}
