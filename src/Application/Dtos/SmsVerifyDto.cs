namespace Authentication.Application.Dtos;

public class SmsVerifyDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}