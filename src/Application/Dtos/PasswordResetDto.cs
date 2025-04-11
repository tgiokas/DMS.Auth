namespace Authentication.Application.Dtos;
public class PasswordResetDto
{
    public string Token { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
}
