namespace Authentication.Application.Dtos;

public class SetupTokenVerifyDto
{
    public string LoginToken { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}