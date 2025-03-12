namespace DMS.Auth.Application.Dtos;

public class TokenDto
{
    public string access_token { get; set; }
    public string Refresh_token { get; set; }
    public string token_type { get; set; }
    public int expires_in { get; set; }
}

