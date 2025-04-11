using Microsoft.AspNetCore.Mvc;

using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;

namespace Authentication.Api.Controllers;

[ApiController]
[Route("api/sms")]
public class SmsVerifyController : ControllerBase
{
    private readonly ISmsVerificationService _smsService;

    public SmsVerifyController(ISmsVerificationService smsVerificationService)
    {
        _smsService = smsVerificationService;
    }

    [HttpPost("send-verification-sms")]
    public async Task<IActionResult> SendVerificationSms([FromBody] SmsSendDto request)
    {
        await _smsService.SendVerificationSmsAsync(request.PhoneNumber);
        return Ok(new { message = "Verification SMS sent" });
    }

    [HttpPost("verify-sms")]
    public async Task<IActionResult> VerifySmsCode([FromBody] SmsVerifyCodeDto request)
    {
        var isValid = await _smsService.VerifySmsAsync(request.PhoneNumber, request.Code);
        if (!isValid)
            return BadRequest(new { message = "Invalid or expired code" });
        
        return Ok(new { message = "Phone number verified successfully" });
    }
}
