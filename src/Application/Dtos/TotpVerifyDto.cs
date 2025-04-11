namespace Authentication.Application.Dtos;

public class TotpVerifyDto
{
    public required string Username { get; set; }
    public required string Code { get; set; }
    public required string SetupToken { get; set; }
}