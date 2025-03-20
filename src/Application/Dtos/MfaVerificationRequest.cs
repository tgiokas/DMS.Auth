namespace DMS.Auth.Application.Dtos;

public class MfaVerificationRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string OtpCode { get; set; }
}