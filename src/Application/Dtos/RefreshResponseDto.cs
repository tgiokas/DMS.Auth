namespace Authentication.Application.Dtos;

public class RefreshResponseDto
{
    public string? Access_token { get; set; }
    public string Refresh_token { get; set; } = default!;
    public int? Expires_in { get; set; }
}