namespace DMS.Auth.Application.Dtos;

public class MfaVerifyDto
{
    public string Username { get; set; }    
    public string OtpCode { get; set; }    
}