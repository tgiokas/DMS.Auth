using System.Text.Json.Serialization;

namespace Authentication.Application.Dtos;

public class RefreshResponseDto
{
    public string? Access_token { get; set; }
    public int? Expires_in { get; set; }
}