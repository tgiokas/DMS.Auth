namespace DMS.Auth.Application.Dtos;

public class TotpVerifyDto
{
    public string Username { get; set; }
    public string Code { get; set; }
    public string? SetupToken { get; set; }
}