namespace Authentication.Application.Dtos;

public class EmailCodeDto
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}