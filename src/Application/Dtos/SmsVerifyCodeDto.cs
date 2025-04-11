namespace Authentication.Application.Dtos;

public class SmsVerifyCodeDto
{
    public string PhoneNumber { get; set; } = default!;
    public string Code { get; set; } = default!;
}