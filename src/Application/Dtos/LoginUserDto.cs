namespace Authentication.Application.Dtos;

public class LoginUserDto
{
    public string? Access_token { get; set; }
    public string? Refresh_token { get; set; }
    public bool Mfa_required { get; set; } = false;
    public string? Mfa_setUp_token { get; set; }
    public int? Expires_in { get; set; }
}