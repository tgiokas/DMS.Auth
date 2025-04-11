namespace Authentication.Application.Dtos;

public class PasswordForgotTokenEntry
{
    public string Email { get; set; } = default!;
    public string UserId { get; set; } = default!;
}
