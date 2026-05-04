namespace Authentication.Application.Dtos;

public class TotpVerifyDto
{   
    public required string Code { get; set; }
    public required string LoginToken { get; set; }
}