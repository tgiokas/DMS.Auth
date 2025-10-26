using Microsoft.AspNetCore.Mvc;

using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;

namespace Authentication.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SmsVerifyController : ControllerBase
{
    private readonly ISmsVerificationService _smsService;

    public SmsVerifyController(ISmsVerificationService smsVerificationService)
    {
        _smsService = smsVerificationService;
    }

    [HttpPost("send-sms")]
    public async Task<IActionResult> SendVerificationSms(SmsSendDto request)
    {
        var result = await _smsService.SendVerificationSmsAsync(request.PhoneNumber);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("verify-sms")]
    public async Task<IActionResult> VerifySmsCode(SmsVerifyDto request)
    {
        var result = await _smsService.VerifySmsAsync(request.PhoneNumber, request.Code);
        if (result.Success)
        {
            return Accepted(result);
        }
        
        return Ok(new { message = "Phone number verified successfully" });
    }
}
